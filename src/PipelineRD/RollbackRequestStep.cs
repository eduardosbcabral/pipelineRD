using PipelineRD.Extensions;

using Polly;
using System;

namespace PipelineRD
{
    public abstract class RollbackRequestStep<TContext> : IRollbackRequestStep<TContext> where TContext : BaseContext
    {
        public Func<TContext, bool> ConditionToExecute { get; set; }
        public Policy Policy { get; set; }
        public TContext Context => _pipeline.Context;
        public int? RollbackIndex { get; private set; }
        public Func<TContext, bool> RequestCondition { get; set; }

        private IPipeline<TContext> _pipeline;
        private IPipelineRequest _request;

        #region Methods
        public TRequest Request<TRequest>() where TRequest : IPipelineRequest
            => (TRequest)(Context.Request ?? _request);

        public void SetPipeline(IPipeline<TContext> pipeline) => _pipeline = pipeline;

        public void SetRequest(IPipelineRequest request) => _request = request;

        public abstract void HandleRollback();
        #endregion

        public void Execute()
        {
            if (RequestCondition != null && !RequestCondition.IsSatisfied(Context))
                return;

            if (ConditionToExecute != null && !ConditionToExecute.IsSatisfied(Context))
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