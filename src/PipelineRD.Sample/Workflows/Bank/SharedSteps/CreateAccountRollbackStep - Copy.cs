namespace PipelineRD.Sample.Workflows.Bank.SharedSteps
{
    public class CreateAccountRollbackStep : RollbackRequestStep<BankContext>, ICreateAccountRollbackStep
    {
        public override void HandleRollback()
        {
        }
    }

    public interface ICreateAccountRollbackStep : IRollbackRequestStep<BankContext>
    { }
}
