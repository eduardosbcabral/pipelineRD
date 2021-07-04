using PipelineRD.Tests.Request;

namespace PipelineRD.Tests.Steps
{
    public class SecondSampleStep : RequestStep<ContextSample>, ISecondSampleStep
    {
        public override RequestStepResult HandleRequest()
        {
            this.Context.SecondWasExecutedCount++;

            if (!this.Request<SampleRequest>().ValidSecond)
            {
                return this.Abort("Error", 400);
            }

            this.Context.SecondWasExecuted = true;

            return this.Next();
        }
    }

    public interface ISecondSampleStep : IRequestStep<ContextSample>
    { }
}
