using PipelineRD;

namespace PipelineRD.Sample;

class CreateAccountRecoveryHandler : RecoveryHandler<AccountContext, AccountRequest>
{
    public override async Task Handle(AccountRequest request)
    {
    }
}

