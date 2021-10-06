
using Polly;

using System;
using System.Linq.Expressions;

namespace PipelineRD
{
    public abstract class RollbackRequestStep<TContext> : IRollbackStep<TContext> where TContext : BaseContext
    {
        public Expression<Func<TContext, bool>> ConditionToExecute { get; set; }
        public Policy Policy { get; set; }
        public int? RollbackIndex { get; private set; }
        public Expression<Func<TContext, bool>> RequestCondition { get; set; }
        public TContext Context { get; private set; }

        private IPipeline<TContext> _pipeline;

        public string Identifier => $"{_pipeline.Identifier}.{GetType().Name}";

        #region Methods
        public TRequest Request<TRequest>() where TRequest : IPipelineRequest
            => Context.Request<TRequest>();

        void IStep<TContext>.SetPipeline(Pipeline<TContext> pipeline) => _pipeline = pipeline;
        public void SetContext(TContext context) => Context = context;

        public abstract void HandleRollback();
        #endregion

        public void Execute()
        {
            if (RequestCondition != null && !RequestCondition.Compile().Invoke(Context))
                return;

            if (ConditionToExecute != null && !ConditionToExecute.Compile().Invoke(Context))
                return;

            if (Policy != null)
            {
                Policy.Execute(() =>
                {
                    HandleRollback();
                });
            }
            else
            {
                HandleRollback();
            }
        }

        public void AddRollbackIndex(int rollbackIndex) 
            => RollbackIndex = rollbackIndex;
    }
}