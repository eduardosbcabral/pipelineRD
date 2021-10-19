using Polly;

using System.Threading.Tasks;

namespace PipelineRD.Async
{
    public interface IAsyncRequestStep<TPipelineContext> : IStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        AsyncPolicy<RequestStepResult> Policy { get; set; }
        Task<RequestStepResult> HandleRequest();
        RequestStepResult Next();
        Task<RequestStepResult> Next(string requestStepHandlerId);
        Task<RequestStepResult> Execute();
    }
}
