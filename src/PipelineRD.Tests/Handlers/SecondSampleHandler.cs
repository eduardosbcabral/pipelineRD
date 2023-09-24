using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class SecondSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override Task Handle(SampleRequest request)
        {
            this.Context.SecondWasExecutedCount++;

            if (!request.ValidSecond)
            {
                Abort("Error", 400);
                return Task.CompletedTask;
            }

            this.Context.SecondWasExecuted = true;

            return Task.CompletedTask;
        }
    }
}
