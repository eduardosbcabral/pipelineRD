using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class FirstSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest request)
        {
            if (!request.ValidFirst)
            {
                return Abort("Error", 400);
            }

            this.Context.FirstWasExecuted = true;

            return Proceed();
        }
    }
}
