using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Async;
using PipelineRD.Builders;
using PipelineRD.Extensions;

using Polly;

using Serilog;
using Serilog.Context;

namespace PipelineRD
{
    public class Pipeline<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
        public TContext Context { get; private set; }
        public string CurrentRequestStepIdentifier { get; private set; }
        public virtual string Identifier => $"Pipeline<{typeof(TContext).Name}>";
        public IReadOnlyCollection<IStep<TContext>> Steps => _requestSteps;

        protected readonly IServiceProvider _serviceProvider;
        protected readonly ICacheProvider _cacheProvider;
        protected string _requestKey;
        protected bool _useReuseRequisitionHash;
        protected bool _finallyStepIsSet = false;
        protected readonly Queue<IStep<TContext>> _requestSteps;
        protected readonly Stack<IRollbackStep<TContext>> _rollbacks;

        #region Constructors
        private Pipeline()
        {
            _useReuseRequisitionHash = true;
            _rollbacks = new Stack<IRollbackStep<TContext>>();
            _requestSteps = new Queue<IStep<TContext>>();
        }

        public Pipeline(IServiceProvider serviceProvider, string requestKey = null) : this()
        {
            _serviceProvider = serviceProvider;
            _requestKey = requestKey;
            _cacheProvider = serviceProvider.GetService<ICacheProvider>();
            Context = serviceProvider.GetService<TContext>();
        }
        #endregion

        public IServiceProvider GetServiceProvider() => _serviceProvider;
        public void SetRequestKey(string requestKey) => _requestKey = requestKey;

        #region RecoveryRequestByHash
        public IPipeline<TContext> EnableRecoveryRequestByHash()
        {
            _useReuseRequisitionHash = true;
            return this;
        }

        public IPipeline<TContext> DisableRecoveryRequestByHash()
        {
            _useReuseRequisitionHash = false;
            return this;
        }
        #endregion

        #region AddNext
        public IPipeline<TContext> AddNext<TRequestStep>() where TRequestStep : IStep<TContext>
        {
            if (_finallyStepIsSet)
            {
                throw new PipelineException("Finally request step is already set. Cannot add a new step.");
            }

            var requestStep = (IStep<TContext>)_serviceProvider.GetService<TRequestStep>();
            if (requestStep == null)
            {
                throw new NullReferenceException("Request step not found.");
            }

            requestStep.SetPipeline(this);

            _requestSteps.Enqueue(requestStep);

            SetCurrentRequestStepIdentifier(requestStep);

            return this;
        }

        public IPipeline<TContext> AddNext<TRequestStep>(IStep<TContext> requestStep) where TRequestStep : IStep<TContext>
        {
            if (requestStep == null)
            {
                throw new NullReferenceException("Request step cannot be null.");
            }

            if (_finallyStepIsSet)
            {
                throw new PipelineException("Finally request step is already set. Cannot add a new step.");
            }

            requestStep.SetPipeline(this);

            _requestSteps.Enqueue(requestStep);

            SetCurrentRequestStepIdentifier(requestStep);

            return this;
        }
        #endregion

        #region AddPolicy
        public IPipeline<TContext> WithPolicy(Policy<RequestStepResult> policy)
        {
            var lastStepRequest = LastStep() as IRequestStep<TContext>;
            if (policy != null && lastStepRequest != null)
            {
                lastStepRequest.Policy = policy;
            }

            return this;
        }

        public IPipeline<TContext> WithPolicy(AsyncPolicy<RequestStepResult> policy)
        {
            var lastStepRequest = LastStep() as IAsyncRequestStep<TContext>;
            if (policy != null && lastStepRequest != null)
            {
                lastStepRequest.Policy = policy;
            }

            return this;
        }
        #endregion

        #region When
        public IPipeline<TContext> When(Expression<Func<TContext, bool>> condition)
        {
            var lastStep = LastStep();
            if (condition != null && lastStep != null)
            {
                lastStep.ConditionToExecute = condition;
            }

            return this;
        }

        public IPipeline<TContext> When<TCondition>()
        {
            var instance = (ICondition<TContext>)_serviceProvider.GetService<TCondition>();
            if (instance == null)
            {
                throw new PipelineException("Could not find the condition. Try adding to the dependency injection container.");
            }

            return When(instance.When());
        }
        #endregion

        #region AddRollback
        public IPipeline<TContext> AddRollback<TRollbackRequestStep>() where TRollbackRequestStep : IRollbackStep<TContext>
        {
            var rollbackStep = (IRollbackStep<TContext>)_serviceProvider.GetService<TRollbackRequestStep>();
            if (rollbackStep == null)
            {
                throw new NullReferenceException("Rollback request step not found.");
            }

            return AddRollback(rollbackStep);
        }

        public IPipeline<TContext> AddRollback(IRollbackStep<TContext> rollbackStep)
        {
            var lastStep = LastStep();
            if (lastStep != null)
            {
                var rollbackIndex = _rollbacks.Count;
                rollbackStep.AddRollbackIndex(rollbackIndex);
                lastStep.AddRollbackIndex(rollbackIndex);
                rollbackStep.ConditionToExecute = lastStep.ConditionToExecute;
                rollbackStep.SetPipeline(this);
                _rollbacks.Push(rollbackStep);
            }

            return this;
        }

        public async Task ExecuteRollback()
        {
            var remainingFirstStepThatHaveRollback = _requestSteps.FirstOrDefault(x => x.RollbackIndex.HasValue);

            var executeRollbackUntilIndex = remainingFirstStepThatHaveRollback != null ?
                remainingFirstStepThatHaveRollback.RollbackIndex :
                _rollbacks.Count;

            foreach(var rollbackStep in _rollbacks.Where(rollbackHandler => rollbackHandler.RollbackIndex < executeRollbackUntilIndex))
            {
                if(rollbackStep.GetType() == typeof(IAsyncRollbackRequestStep<TContext>))
                {
                    await ((IAsyncRollbackRequestStep<TContext>)rollbackStep).Execute();
                }
                else
                {
                    ((IRollbackRequestStep<TContext>)rollbackStep).Execute();
                }
            }
        }
        #endregion

        #region AddFinally
        public IPipeline<TContext> AddFinally<TRequestStep>() where TRequestStep : IStep<TContext>
        {
            AddNext<TRequestStep>();
            _finallyStepIsSet = true;
            return this;
        }
        #endregion

        #region Execute
        public async Task<RequestStepResult> Execute<TRequest>(TRequest request) where TRequest : IPipelineRequest
            => await Execute (request, string.Empty);

        public async Task<RequestStepResult> Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : IPipelineRequest
        {
            var headStep = HeadStep();
            if (headStep == null)
            {
                throw new NullReferenceException("There are no steps in the pipeline.");
            }

            SetCurrentRequestStepIdentifier(headStep);

            var hash = string.IsNullOrEmpty(idempotencyKey) ?
                    request.GenerateHash(Identifier) :
                    idempotencyKey;
            var firstStepIdentifier = string.Empty;

            if (_useReuseRequisitionHash)
            {
                var snapshot = _cacheProvider.Get<PipelineSnapshot>(hash).Result;
                if (snapshot != null)
                {
                    if (snapshot.Success)
                    {
                        return snapshot.Context.Response;
                    }
                    else
                    {
                        Context = (TContext)snapshot.Context;
                        firstStepIdentifier = snapshot.LastRequestIdentifier;
                    }
                }
            }

            // Set the Request in the shared Context
            Context.SetRequest(request);

            var pipelineResult = await ExecutePipeline(firstStepIdentifier);

            if (pipelineResult == null)
            {
                throw new PipelineException("The pipeline did not returned a result. End the pipeline using the method 'Finish'.");
            }

            if (_useReuseRequisitionHash)
            {
                var snapshot = new PipelineSnapshot(
                    pipelineResult.IsSuccess(),
                    CurrentRequestStepIdentifier,
                    Context);

                await _cacheProvider.Add(snapshot, hash);
            }

            return pipelineResult;
        }

        public async Task<RequestStepResult> ExecuteFromSpecificRequestStep(string requestStepIdentifier)
        {
            if (CurrentRequestStepIdentifier.Equals(requestStepIdentifier, StringComparison.InvariantCultureIgnoreCase))
            {
                return await ExecuteNextRequestStep();
            }

            DequeueCurrentStep();
            SetCurrentRequestStepIdentifier(HeadStep());
            return await ExecuteFromSpecificRequestStep(requestStepIdentifier);
        }

        public async Task<RequestStepResult> ExecuteNextRequestStep() 
        {
            var currentStep = DequeueCurrentStep();
            SetCurrentRequestStepIdentifier(currentStep);

            if (currentStep.ConditionToExecute is null || currentStep.ConditionToExecute.Compile().Invoke(currentStep.Context))
            {
                RequestStepResult result;

                if(currentStep.GetType() == typeof(IAsyncRequestStep<TContext>))
                {
                    result = await ((IAsyncRequestStep<TContext>)currentStep).Execute();
                } 
                else
                {
                    result = ((IRequestStep<TContext>)currentStep).Execute();
                }

                return result;
            }

            return await ExecuteNextRequestStep();
        }

        private async Task<RequestStepResult> ExecuteStep(IAsyncRequestStep<TContext> step)
            => await step.Execute();

        private RequestStepResult ExecuteStep(IRequestStep<TContext> step)
            => step.Execute();
        #endregion

        #region Protected Methods
        private async Task<RequestStepResult> ExecutePipeline(string firstStepIdentifier)
        {
            RequestStepResult pipelineResult = null;

            try
            {
                if (!string.IsNullOrEmpty(firstStepIdentifier))
                {
                    pipelineResult = await ExecuteFromSpecificRequestStep(firstStepIdentifier);
                }
                else
                {
                    pipelineResult = await ExecuteNextRequestStep();
                }
            }
            catch (PipelinePolicyException pipelinePolicyException)
            {
                pipelineResult = pipelinePolicyException.Result;
            }
            catch (Exception ex)
            {
                if (Log.Logger != null)
                {
                    using (LogContext.PushProperty("RequestKey", _requestKey))
                    {
                        Log.Logger.Error(ex, $"Error - {CurrentRequestStepIdentifier}");
                    }
                }
            }
            finally
            {
                if (_finallyStepIsSet) {
                    pipelineResult = await ExecuteFinallyHandler();
                }
            }

            return pipelineResult;
        }

        private async Task<RequestStepResult> ExecuteFinallyHandler()
        {
            RequestStepResult result = null;

            var lastStep = LastStep();
            if (lastStep != null)
            {
                if (lastStep.GetType() == typeof(IAsyncRequestStep<TContext>))
                {
                    result = await ((IAsyncRequestStep<TContext>)lastStep).Execute();
                }
                else
                {
                    result = ((IRequestStep<TContext>)lastStep).Execute();
                }
            }

            return result;
        }

        protected IStep<TContext> LastStep() => _requestSteps.LastOrDefault();
        protected IStep<TContext> HeadStep() => _requestSteps.FirstOrDefault();
        private IStep<TContext> DequeueCurrentStep()
            => _requestSteps.Dequeue();

        private void SetCurrentRequestStepIdentifier(IStep<TContext> step)
        {
            CurrentRequestStepIdentifier = step.Identifier;
        }
        #endregion
    }
}
