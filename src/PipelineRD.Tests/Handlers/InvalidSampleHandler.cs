using PipelineRD.Tests.Request;

namespace PipelineRD.Tests.Handlers
{
    public class InvalidSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override void Handle(SampleRequest _)
        {
            throw new System.NotImplementedException();
        }
    }
}
