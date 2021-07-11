using Polly;

using System.Threading.Tasks;

namespace PipelineRD
{
    public interface IAsyncRollbackRequestStep<TPipelineContext> : IRollbackStep<TPipelineContext> where TPipelineContext : BaseContext
    {
        AsyncPolicy Policy { get; set; }
        Task HandleRollback();
        Task Execute();
    }
}