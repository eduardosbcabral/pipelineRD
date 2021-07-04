namespace PipelineRD.Tests.Steps
{
    public class FirstSampleRollbackStep : RollbackRequestStep<ContextSample>, IFirstSampleRollbackStep
    {
        public override void HandleRollback()
        {
            this.Context.FirstRollbackWasExecuted = true;
        }
    }

    public interface IFirstSampleRollbackStep : IRollbackRequestStep<ContextSample>
    { }
}
