using Polly;

using System;
using System.Net;

namespace PipelineRD
{
    public abstract class RequestStep<TContext> : IRequestStep<TContext> where TContext : BaseContext
    {
        public Policy<RequestStepResult> Policy { get; set; }
        public TContext Context => _pipeline?.Context;
        public Func<TContext, bool> ConditionToExecute { get; set; }
        public int? RollbackIndex { get; private set; }

        private IPipeline<TContext> _pipeline;
        private IPipelineRequest _request;

        private const int DEFAULT_FAILURE_STATUS_CODE = 400;
        private const int DEFAULT_SUCCESS_STATUS_CODE = 200;

        public string Identifier => $"{_pipeline.Identifier}.{GetType().Name}";

        #region Constructors
        protected RequestStep()
        { }
        #endregion

        #region Methods

        public TRequest Request<TRequest>() where TRequest : IPipelineRequest
            => (TRequest)(Context.Request ?? _request);

        public void SetPipeline(IPipeline<TContext> pipeline) => _pipeline = pipeline;

        public void SetRequest(IPipelineRequest request) => _request = request;

        public abstract RequestStepResult HandleRequest();
        #endregion

        #region Next
        public RequestStepResult Next()
           => Next(string.Empty);

        public RequestStepResult Next(string requestHandlerIdentifier)
        {
            if (!string.IsNullOrEmpty(requestHandlerIdentifier))
            {
                return _pipeline.ExecuteFromSpecificRequestStep(requestHandlerIdentifier);
            }

            return _pipeline.ExecuteNextRequestStep();
        }

        /// <summary>
        /// This method receive a request step identifier and will execute the step if it finds it
        /// </summary>
        /// <param name="requestStepIdentifier"></param>
        /// <returns></returns>
        public RequestStepResult ProceedToStep(string requestStepIdentifier)
        {
            if (!string.IsNullOrEmpty(requestStepIdentifier))
            {
                return _pipeline.ExecuteFromSpecificRequestStep(requestStepIdentifier);
            }

            return _pipeline.ExecuteNextRequestStep();
        }
        #endregion

        #region Abort
        protected RequestStepResult Abort(string errorMessage, int statusCode)
            => Context.Response = BaseAbort(errorMessage: errorMessage, statusCode: statusCode);

        protected RequestStepResult Abort(string errorMessage, HttpStatusCode httpStatusCode)
            => Context.Response = BaseAbort(errorMessage: errorMessage, statusCode: (int)httpStatusCode);

        protected RequestStepResult Abort(string errorMessage)
            => BaseAbort(errorMessage: errorMessage);

        protected RequestStepResult Abort(object errorResult, int statusCode)
            => Context.Response = BaseAbort(errorResultObject: errorResult, statusCode: statusCode);

        protected RequestStepResult Abort(object errorResult, HttpStatusCode httpStatusCode)
            => Context.Response = BaseAbort(errorResultObject: errorResult, statusCode: (int)httpStatusCode);

        protected RequestStepResult Abort(object errorResult)
            => Context.Response = BaseAbort(errorResultObject: errorResult);

        protected RequestStepResult Abort(RequestError errorResult, int statusCode)
            => Context.Response = BaseAbort(errorResult: errorResult, statusCode: statusCode);

        protected RequestStepResult Abort(RequestError errorResult, HttpStatusCode httpStatusCode)
            => Context.Response = BaseAbort(errorResult: errorResult, statusCode: (int)httpStatusCode);

        protected RequestStepResult Abort(RequestError errorResult)
            => Context.Response = BaseAbort(errorResult: errorResult);

        private RequestStepResult BaseAbort(
            string errorMessage = "",
            int statusCode = DEFAULT_FAILURE_STATUS_CODE,
            object errorResultObject = null,
            RequestError errorResult = null)
            => Context.Response = RequestStepHandlerResultBuilder.Instance()
                .WithErrorMessage(errorMessage)
                .WithStatusCode(statusCode)
                .WithResultObject(errorResultObject)
                .WithErrors(errorResult)
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
            _pipeline.ExecuteRollback();
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
