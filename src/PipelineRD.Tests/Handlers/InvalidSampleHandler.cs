using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class InvalidSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest _)
        {
            throw new System.NotImplementedException();
        }
    }
}
