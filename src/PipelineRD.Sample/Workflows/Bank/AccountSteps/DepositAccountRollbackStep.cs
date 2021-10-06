namespace PipelineRD.Sample.Workflows.Bank.AccountSteps
{
    public class DepositAccountRollbackStep : RollbackRequestStep<BankContext>, IDepositAccountRollbackStep
    {
        public override void HandleRollback()
        {
        }
    }

    public interface IDepositAccountRollbackStep : IRollbackStep<BankContext>
    { }
}
