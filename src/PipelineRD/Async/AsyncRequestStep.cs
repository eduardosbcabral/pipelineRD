using Polly;

using System;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace PipelineRD.Async
{
    public abstract class AsyncRequestStep<TContext> : IAsyncRequestStep<TContext> where TContext : BaseContext
    {
        public AsyncPolicy<RequestStepResult> Policy { get; set; }
        public Expression<Func<TContext, bool>> ConditionToExecute { get; set; }
        public int? RollbackIndex { get; private set; }
        public TContext Context { get; private set; }

        private Pipeline<TContext> _pipeline;

        private const int DEFAULT_FAILURE_STATUS_CODE = 400;
        private const int DEFAULT_SUCCESS_STATUS_CODE = 200;

        public string Identifier => $"{_pipeline.Identifier}.{GetType().Name}";

        #region Constructors
        protected AsyncRequestStep()
        { }
        #endregion

        #region Methods

        public TRequest Request<TRequest>() where TRequest : IPipelineRequest
            => Context.Request<TRequest>();

        void IStep<TContext>.SetPipeline(Pipeline<TContext> pipeline) => _pipeline = pipeline;
        public void SetContext(TContext context) => Context = context;

        public abstract Task<RequestStepResult> HandleRequest();
        #endregion

        #region Next
        public async Task<RequestStepResult> Next()
           => await Next(string.Empty);

        public async Task<RequestStepResult> Next(string requestHandlerIdentifier)
        {
            if (!string.IsNullOrEmpty(requestHandlerIdentifier))
            {
                return await _pipeline.ExecuteFromSpecificRequestStep(requestHandlerIdentifier);
            }

            return await _pipeline.ExecuteNextRequestStep();
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
        public async Task<RequestStepResult> Execute()
        {
            RequestStepResult result = null;

            if (Policy != null)
            {
                result = await Policy.ExecuteAsync(async () =>
                {
                    if (typeof(Policy) == typeof(Policy<RequestStepResult>))
                    {
                        // Se existir resposta E não for sucedida E a step atual for diferente da step da resposta no contexto
                        if (Context.Response != null && !Context.Response.IsSuccess() && Context.Response.RequestStepIdentifier != Identifier)
                        {
                            throw new PipelinePolicyException(Context.Response);
                        }
                    }

                    return await HandleRequest();
                });
      
            }
            else
            {
                result = await HandleRequest();
            }

            return result;
        }
        #endregion

        #region Rollback
        protected async Task<RequestStepResult> Rollback(object result, int statusCode)
            => await Rollback(BaseFinish(result: result, statusCode: statusCode));

        protected async Task<RequestStepResult> Rollback(object result, HttpStatusCode httpStatusCode)
            => await Rollback(BaseFinish(result: result, statusCode: (int)httpStatusCode));

        protected async Task<RequestStepResult> Rollback(int statusCode)
            => await Rollback(BaseFinish(statusCode: statusCode));

        protected async Task<RequestStepResult> Rollback(object result)
            => await Rollback(BaseFinish(result));

        private async Task<RequestStepResult> Rollback(RequestStepResult result)
        {
            await _pipeline.ExecuteRollback();
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
