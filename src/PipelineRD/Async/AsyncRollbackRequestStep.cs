
using Polly;

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PipelineRD
{
    public abstract class AsyncRollbackRequestStep<TContext> : IAsyncRollbackRequestStep<TContext> where TContext : BaseContext
    {
        public Expression<Func<TContext, bool>> ConditionToExecute { get; set; }
        public AsyncPolicy Policy { get; set; }
        public TContext Context => _pipeline.Context;
        public int? RollbackIndex { get; private set; }
        public Expression<Func<TContext, bool>> RequestCondition { get; set; }

        private IPipeline<TContext> _pipeline;
        private IPipelineRequest _request;

        #region Methods
        public TRequest Request<TRequest>() where TRequest : IPipelineRequest
            => (TRequest)(Context.Request ?? _request);

        public void SetPipeline(IPipeline<TContext> pipeline) => _pipeline = pipeline;

        public void SetRequest(IPipelineRequest request) => _request = request;

        public abstract Task HandleRollback();
        #endregion

        public async Task Execute()
        {
            if (RequestCondition != null && !RequestCondition.Compile().Invoke(Context))
                return;

            if (ConditionToExecute != null && !ConditionToExecute.Compile().Invoke(Context))
                return;

            if (Policy != null)
            {
                await Policy.ExecuteAsync(async () => await HandleRollback());
            }
            else
            {
                await HandleRollback();
            }
        }

        public void AddRollbackIndex(int rollbackIndex) 
            => RollbackIndex = rollbackIndex;
    }
}