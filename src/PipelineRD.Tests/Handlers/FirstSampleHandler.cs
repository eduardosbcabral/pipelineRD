using PipelineRD.Tests.Request;

namespace PipelineRD.Tests.Handlers
{
    public class FirstSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override void Handle(SampleRequest request)
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
