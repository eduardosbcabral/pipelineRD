using Polly;

namespace PipelineRD
{
    public interface IRequestStep<TPipelineContext> : IStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        Policy<RequestStepResult> Policy { get; set; }
        string Identifier { get; }
        RequestStepResult HandleRequest();
        RequestStepResult Next();
        RequestStepResult Next(string requestStepHandlerId);
        RequestStepResult Execute();
    }
}
