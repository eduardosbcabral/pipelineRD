using PipelineRD.Tests.Request;

namespace PipelineRD.Tests.Handlers
{
    public class ThirdSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override void Handle(SampleRequest _)
        {
            this.Context.ThirdWasExecuted = true;

            this.Finish(null, 200);
        }
    }
}
