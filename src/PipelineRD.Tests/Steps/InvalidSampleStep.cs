namespace PipelineRD.Tests.Steps
{
    public class InvalidSampleStep : RequestStep<ContextSample>, IInvalidSampleStep
    {
        public override RequestStepResult HandleRequest()
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IInvalidSampleStep : IRequestStep<ContextSample>
    {
    }
}
