using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class SecondSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override async Task Handle(SampleRequest request)
        {
            this.Context.SecondWasExecutedCount++;

            if (!request.ValidSecond)
            {
                this.Abort("Error", 400);
                return;
            }

            this.Context.SecondWasExecuted = true;
        }
    }
}
