using Polly;
using System;

namespace PipelineRD
{
    public interface IStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        TPipelineContext Context { get; }
        Func<TPipelineContext, bool> ConditionToExecute { get; set; }
        void SetPipeline(IPipeline<TPipelineContext> pipeline);
        TRequest Request<TRequest>() where TRequest : IPipelineRequest;
        void AddRollbackIndex(int index);
        int? RollbackIndex { get; }
    }
}