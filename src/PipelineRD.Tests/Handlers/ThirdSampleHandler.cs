using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class ThirdSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest _)
        {
            this.Context.ThirdWasExecuted = true;

            return this.Finish(null, 200);
        }
    }
}
