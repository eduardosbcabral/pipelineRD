using System;

namespace PipelineRD.Tests.Steps
{
    public class ThirdSampleStep : RequestStep<ContextSample>, IThirdSampleStep
    {
        public override RequestStepResult HandleRequest()
        {
            this.Context.ThirdWasExecuted = true;

            return this.Finish(200);
        }
    }

    public interface IThirdSampleStep : IRequestStep<ContextSample>
    { }
}
