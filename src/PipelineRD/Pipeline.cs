using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

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
        public string Identifier => $"Pipeline<{typeof(TContext).Name}>";
        public IReadOnlyCollection<IRequestStep<TContext>> Steps => _requestSteps;

        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheProvider _cacheProvider;
        private IValidator _validator;
        private readonly string _requestKey;
        private bool _useReuseRequisitionHash;
        private bool _finallyStepIsSet = false;
        private readonly Queue<IRequestStep<TContext>> _requestSteps;
        private readonly Stack<IRollbackRequestStep<TContext>> _rollbacks;

        #region Constructors
        protected Pipeline()
        {
            _useReuseRequisitionHash = true;
            _rollbacks = new Stack<IRollbackRequestStep<TContext>>();
            _requestSteps = new Queue<IRequestStep<TContext>>();
        }

        public Pipeline(IServiceProvider serviceProvider, string requestKey = null) : this()
        {
            _serviceProvider = serviceProvider;
            _requestKey = requestKey;
            _cacheProvider = serviceProvider.GetService<ICacheProvider>();
            Context = serviceProvider.GetService<TContext>();
        }
        #endregion

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
        public IPipeline<TContext> AddNext<TRequestStep>() where TRequestStep : IRequestStep<TContext>
        {
            if (_finallyStepIsSet)
            {
                throw new PipelineException("Finally request step is already set. Cannot add a new step.");
            }

            var requestStep = (IRequestStep<TContext>)_serviceProvider.GetService<TRequestStep>();
            if (requestStep == null)
            {
                throw new NullReferenceException("Request step not found.");
            }

            requestStep.SetPipeline(this);

            _requestSteps.Enqueue(requestStep);

            SetCurrentRequestStepIdentifier(requestStep);

            return this;
        }
        #endregion

        #region AddValidator
        public IPipeline<TContext> AddValidator<TRequest>(IValidator<TRequest> validator) where TRequest : IPipelineRequest
        {
            _validator = validator;
            return this;
        }

        public IPipeline<TContext> AddValidator<TRequest>() where TRequest : IPipelineRequest
        {
            var validator = _serviceProvider.GetService<IValidator<TRequest>>();
            return AddValidator(validator);
        }
        #endregion

        #region AddPolicy
        public IPipeline<TContext> WithPolicy(Policy<RequestStepResult> policy)
        {
            var lastStepRequest = LastStep();
            if (policy != null && lastStepRequest != null)
            {
                lastStepRequest.Policy = policy;
            }

            return this;
        }
        #endregion

        #region When
        public IPipeline<TContext> When(Func<TContext, bool> condition)
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
        public IPipeline<TContext> AddRollback<TRollbackRequestStep>() where TRollbackRequestStep : IRollbackRequestStep<TContext>
        {
            var rollbackStep = (IRollbackRequestStep<TContext>)_serviceProvider.GetService<TRollbackRequestStep>();
            if (rollbackStep == null)
            {
                throw new NullReferenceException("Rollback request step not found.");
            }
            return AddRollback(rollbackStep);
        }

        public IPipeline<TContext> AddRollback(IRollbackRequestStep<TContext> rollbackStep)
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

        public void ExecuteRollback()
        {
            var remainingFirstStepThatHaveRollback = _requestSteps.FirstOrDefault(x => x.RollbackIndex.HasValue);

            var executeRollbackUntilIndex = remainingFirstStepThatHaveRollback != null ?
                remainingFirstStepThatHaveRollback.RollbackIndex :
                _rollbacks.Count;

            foreach(var rollbackStep in _rollbacks.Where(rollbackHandler => rollbackHandler.RollbackIndex < executeRollbackUntilIndex))
            {
                rollbackStep.Execute();
            }
        }
        #endregion

        #region AddFinally
        public IPipeline<TContext> AddFinally<TRequestStep>() where TRequestStep : IRequestStep<TContext>
        {
            AddNext<TRequestStep>();
            _finallyStepIsSet = true;
            return this;
        }
        #endregion

        #region Execute
        public RequestStepResult Execute<TRequest>(TRequest request) where TRequest : IPipelineRequest
            => Execute(request, string.Empty);

        public RequestStepResult Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : IPipelineRequest
        {
            var headStep = HeadStep();
            if (headStep == null)
            {
                throw new NullReferenceException("There are no steps in the pipeline.");
            }

            SetCurrentRequestStepIdentifier(headStep);

            if (_validator != null)
            {
                var validationContext = new ValidationContext<TRequest>(request);
                var validateResult = _validator.Validate(validationContext);

                if (!validateResult.IsValid)
                {
                    var errors = validateResult.Errors
                        .Select(p => RequestErrorBuilder.Instance()
                            .WithMessage(p.ErrorMessage)
                            .WithProperty(p.PropertyName)
                            .Build())
                        .ToArray();

                    return RequestStepHandlerResultBuilder.Instance()
                        .WithErrors(errors)
                        .WithHttpStatusCode(HttpStatusCode.BadRequest)
                        .Build();
                }
            }

            var hash = string.IsNullOrEmpty(idempotencyKey) ?
                    request.GenerateHash(Identifier) :
                    idempotencyKey;
            var firstStepIdentifier = string.Empty;

            // Quando pedir para executar, verificar se estamos utilizando o recovery
            if (_useReuseRequisitionHash)
            {
                // Se existir um snapshot com o hash, irá voltar para a step na pipeline de onde parou a última execução falha.
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
            Context.Request = request;

            var pipelineResult = ExecutePipeline(firstStepIdentifier);

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

                _cacheProvider.Add(snapshot, hash);
            }

            return pipelineResult;
        }

        public RequestStepResult ExecuteFromSpecificRequestStep(string requestStepIdentifier)
        {
            if (CurrentRequestStepIdentifier.Equals(requestStepIdentifier, StringComparison.InvariantCultureIgnoreCase))
            {
                return ExecuteNextRequestStep();
            }

            DequeueCurrentStep();
            SetCurrentRequestStepIdentifier(HeadStep());
            return ExecuteFromSpecificRequestStep(requestStepIdentifier);
        }

        public RequestStepResult ExecuteNextRequestStep() 
        {
            var currentStep = DequeueCurrentStep();
            SetCurrentRequestStepIdentifier(currentStep);

            if (currentStep.ConditionToExecute is null || currentStep.ConditionToExecute.IsSatisfied(currentStep.Context))
            {
                var result = currentStep.Execute();
                return result;
            }

            return ExecuteNextRequestStep();
        }
        #endregion

        #region Private Methods
        private RequestStepResult ExecutePipeline(string firstStepIdentifier)
        {
            RequestStepResult pipelineResult = null;

            try
            {
                if (!string.IsNullOrEmpty(firstStepIdentifier))
                {
                    pipelineResult = ExecuteFromSpecificRequestStep(firstStepIdentifier);
                }
                else
                {
                    pipelineResult = ExecuteNextRequestStep();
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
                    pipelineResult = ExecuteFinallyHandler();
                }
            }

            return pipelineResult;
        }

        private RequestStepResult ExecuteFinallyHandler()
        {
            RequestStepResult result = null;

            var lastStep = LastStep();
            if (lastStep != null)
            {
                result = lastStep.Execute();
            }

            return result;
        }

        private IRequestStep<TContext> LastStep() => _requestSteps.LastOrDefault();
        private IRequestStep<TContext> HeadStep() => _requestSteps.FirstOrDefault();
        private IRequestStep<TContext> DequeueCurrentStep()
            => _requestSteps.Dequeue();

        private void SetCurrentRequestStepIdentifier(IRequestStep<TContext> step)
        {
            CurrentRequestStepIdentifier = step.Identifier;
        }
        #endregion
    }
}
