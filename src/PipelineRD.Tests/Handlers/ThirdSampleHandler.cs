using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class ThirdSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override async Task Handle(SampleRequest _)
        {
            this.Context.ThirdWasExecuted = true;

            this.Finish(null, 200);
        }
    }
}
