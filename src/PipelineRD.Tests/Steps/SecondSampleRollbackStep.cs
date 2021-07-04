namespace PipelineRD.Tests.Steps
{
    public class SecondSampleRollbackStep : RollbackRequestStep<ContextSample>, ISecondSampleRollbackStep
    {
        public override void HandleRollback()
        {
            this.Context.SecondRollbackWasExecuted = true;
        }
    }

    public interface ISecondSampleRollbackStep : IRollbackRequestStep<ContextSample>
    { }
}
