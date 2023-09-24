using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class FirstSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override async Task Handle(SampleRequest request)
        {
            if (!request.ValidFirst)
            {
                this.Abort("Error", 400);
                return;
            }

            this.Context.FirstWasExecuted = true;
        }
    }
}
