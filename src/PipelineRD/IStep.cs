using System;
using System.Linq.Expressions;

namespace PipelineRD
{
    public interface IStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        string Identifier { get; }
        TPipelineContext Context { get; }
        TRequest Request<TRequest>() where TRequest : IPipelineRequest;
        Expression<Func<TPipelineContext, bool>> ConditionToExecute { get; set; }
        internal void SetPipeline(Pipeline<TPipelineContext> pipeline);
        void SetContext(TPipelineContext context);
        void AddRollbackIndex(int index);
        int? RollbackIndex { get; }
    }
}