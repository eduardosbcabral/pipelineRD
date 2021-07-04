using Polly;
using System;
using System.Linq.Expressions;

namespace PipelineRD
{
    public interface IStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        TPipelineContext Context { get; }
        Expression<Func<TPipelineContext, bool>> ConditionToExecute { get; set; }
        void SetPipeline(IPipeline<TPipelineContext> pipeline);
        TRequest Request<TRequest>() where TRequest : IPipelineRequest;
        void AddRollbackIndex(int index);
        int? RollbackIndex { get; }
    }
}