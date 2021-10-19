
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
        public int? RollbackIndex { get; private set; }
        public Expression<Func<TContext, bool>> RequestCondition { get; set; }
        public TContext Context { get; private set; }

        private IPipeline<TContext> _pipeline;
        private object _request;

        public string Identifier => $"{_pipeline.Identifier}.{GetType().Name}";

        #region Methods
        public TRequest Request<TRequest>()
            => Context.Request<TRequest>();

        void IStep<TContext>.SetPipeline(Pipeline<TContext> pipeline) => _pipeline = pipeline;
        public void SetContext(TContext context) => Context = context;

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