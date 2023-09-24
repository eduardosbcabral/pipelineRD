using PipelineRD.Tests.Request;
using System.Threading.Tasks;

namespace PipelineRD.Tests.Handlers
{
    public class FirstSampleHandler : Handler<ContextSample, SampleRequest>
    {
        public override Task Handle(SampleRequest request)
        {
            if (!request.ValidFirst)
            {
                Abort("Error", 400);
                return Task.CompletedTask;
            }

            this.Context.FirstWasExecuted = true;
            return Task.CompletedTask;
        }
    }
}
