namespace PipelineRD.Tests.Steps
{
    public class RollbackSampleStep : RequestStep<ContextSample>, IRollbackSampleStep
    {
        public override RequestStepResult HandleRequest()
        {
            this.Context.RollbackWasExecuted = true;

            return this.Rollback(201);
        }
    }

    public interface IRollbackSampleStep : IRequestStep<ContextSample>
    { }
}
