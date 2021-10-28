using Polly;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace PipelineRD
{
    public abstract class RequestStep<TContext> : IRequestStep<TContext> where TContext : BaseContext
    {
        public Policy<RequestStepResult> Policy { get; set; }
        public Expression<Func<TContext, bool>> ConditionToExecute { get; set; }
        public int? RollbackIndex { get; private set; }
        public TContext Context { get; private set; }

        private object _request;
        private Pipeline<TContext> _pipeline;

        private const int DEFAULT_FAILURE_STATUS_CODE = 400;
        private const int DEFAULT_SUCCESS_STATUS_CODE = 200;

        public string Identifier => $"{_pipeline.Identifier}.{GetType().Name}";

        #region Constructors
        protected RequestStep()
        { }
        #endregion

        #region Methods

        public TRequest Request<TRequest>()
            => Context.Request<TRequest>();

        void IStep<TContext>.SetPipeline(Pipeline<TContext> pipeline) => _pipeline = pipeline;
        public void SetContext(TContext context) => Context = context;

        public abstract RequestStepResult HandleRequest();
        #endregion

        #region Next
        public RequestStepResult Next()
           => Next(string.Empty);

        public RequestStepResult Next(string requestHandlerIdentifier)
        {
            if(_pipeline == null)
            {
                return RequestStepHandlerResultBuilder.Instance()
                    .WithResultObject("Next")
                    .WithSuccess()
                    .Build();
            }

            if (!string.IsNullOrEmpty(requestHandlerIdentifier))
            {
                return Task.Run(() => _pipeline.ExecuteFromSpecificRequestStep(requestHandlerIdentifier)).GetAwaiter().GetResult();
            }

            return Task.Run(() => _pipeline.ExecuteNextRequestStep()).GetAwaiter().GetResult();
        }
        #endregion

        #region Abort
        protected RequestStepResult Abort(string errorMessage, HttpStatusCode httpStatusCode)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithErrorMessage(errorMessage)
                .WithStatusCode((int)httpStatusCode)
                .WithFailure()
                .WithRequestStepIdentifier(Identifier)
                .Build();

        protected RequestStepResult Abort(string errorMessage, int httpStatusCode)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithErrorMessage(errorMessage)
                .WithStatusCode(httpStatusCode)
                .WithFailure()
                .WithRequestStepIdentifier(Identifier)
                .Build();

        protected RequestStepResult Abort(RequestError errorResult, HttpStatusCode httpStatusCode)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithError(errorResult)
                .WithStatusCode((int)httpStatusCode)
                .WithFailure()
                .WithRequestStepIdentifier(Identifier)
                .Build();

        protected RequestStepResult Abort(RequestError errorResult, int httpStatusCode)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithError(errorResult)
                .WithStatusCode(httpStatusCode)
                .WithFailure()
                .WithRequestStepIdentifier(Identifier)
                .Build();

        protected RequestStepResult Abort(List<RequestError> errorsResult, int httpStatusCode)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithErrors(errorsResult)
                .WithStatusCode(httpStatusCode)
                .WithFailure()
                .WithRequestStepIdentifier(Identifier)
                .Build();
        #endregion

        #region Execute
        public RequestStepResult Execute()
        {
            RequestStepResult result = null;

            if (Policy != null)
            {
                result = Policy.Execute(() =>
                {
                    if (typeof(Policy) == typeof(Policy<RequestStepResult>))
                    {
                        // Se existir resposta E não for sucedida E a step atual for diferente da step da resposta no contexto
                        if (Context.Response != null && !Context.Response.IsSuccess() && Context.Response.RequestStepIdentifier != Identifier)
                        {
                            throw new PipelinePolicyException(Context.Response);
                        }
                    }

                    return HandleRequest();
                });

            }
            else
            {
                result = HandleRequest();
            }

            return result;
        }
        #endregion

        #region Rollback
        protected RequestStepResult Rollback(object result, int statusCode)
            => Rollback(BaseFinish(result: result, statusCode: statusCode));

        protected RequestStepResult Rollback(object result, HttpStatusCode httpStatusCode)
            => Rollback(BaseFinish(result: result, statusCode: (int)httpStatusCode));

        protected RequestStepResult Rollback(int statusCode)
            => Rollback(BaseFinish(statusCode: statusCode));

        protected RequestStepResult Rollback(object result)
            => Rollback(BaseFinish(result));

        private RequestStepResult Rollback(RequestStepResult result)
        {
            Task.Run(() => _pipeline.ExecuteRollback()).GetAwaiter().GetResult();
            return result;
        }
        #endregion

        #region Finish
        protected RequestStepResult Finish(object result, int statusCode)
            => BaseFinish(result: result, statusCode: statusCode);

        protected RequestStepResult Finish(object result, HttpStatusCode httpStatusCode)
            => BaseFinish(result: result, statusCode: (int)httpStatusCode);

        protected RequestStepResult Finish(object result)
            => BaseFinish(result);

        private RequestStepResult BaseFinish(
            object result = null,
            int statusCode = DEFAULT_SUCCESS_STATUS_CODE)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithResultObject(result)
                .WithStatusCode(statusCode)
                .WithSuccess()
                .WithRequestStepIdentifier(Identifier)
                .Build();
        #endregion

        public void AddRollbackIndex(int rollbackIndex)
            => RollbackIndex = rollbackIndex;
    }
}
