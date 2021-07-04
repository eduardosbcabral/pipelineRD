using Polly;

namespace PipelineRD
{
    public interface IRollbackRequestStep<TPipelineContext> : IStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        Policy Policy { get; set; }
        void HandleRollback();
        void Execute();
    }
}