using PipelineRD;

namespace PipelineRD.Sample;

class CreateAccountRecoveryHandler : RecoveryHandler<AccountContext, AccountRequest>
{
    public override void Handle(AccountRequest request)
    {
    }
}

