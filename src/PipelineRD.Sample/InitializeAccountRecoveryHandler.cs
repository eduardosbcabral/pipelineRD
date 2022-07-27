using PipelineRD;

namespace PipelineRD.Sample;

class InitializeAccountRecoveryHandler : RecoveryHandler<AccountContext, AccountRequest>
{
    public override void Handle(AccountRequest request)
    {
    }
}

