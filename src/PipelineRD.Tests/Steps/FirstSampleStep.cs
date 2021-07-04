using PipelineRD.Tests.Request;

namespace PipelineRD.Tests.Steps
{
    public class FirstSampleStep : RequestStep<ContextSample>, IFirstSampleStep
    {
        public override RequestStepResult HandleRequest()
        {
            if (!this.Request<SampleRequest>().ValidFirst)
            {
                return this.Abort("Error", 400);
            }

            this.Context.FirstWasExecuted = true;

            return this.Next();
        }
    }

    public interface IFirstSampleStep : IRequestStep<ContextSample>
    { }
}
