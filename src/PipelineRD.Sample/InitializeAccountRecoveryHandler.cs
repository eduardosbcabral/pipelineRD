namespace PipelineRD.Sample;

class InitializeAccountRecoveryHandler : RecoveryHandler<AccountContext, AccountRequest>
{
    public override Task Handle(AccountRequest request)
    {
        return Proceed();
    }
}

