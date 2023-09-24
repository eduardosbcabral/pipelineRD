using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class SecondSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest request)
        {
            this.Context.SecondWasExecutedCount++;

            if (!request.ValidSecond)
            {
                return Abort("Error", 400);
            }

            this.Context.SecondWasExecuted = true;

            return Proceed();
        }
    }
}
