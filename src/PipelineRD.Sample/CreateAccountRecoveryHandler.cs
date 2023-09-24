using PipelineRD;

namespace PipelineRD.Sample;

class CreateAccountRecoveryHandler : RecoveryHandler<AccountContext, AccountRequest>
{
    public override Task Handle(AccountRequest request)
    {
        return Task.CompletedTask;
    }
}

