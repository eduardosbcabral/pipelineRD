using PipelineRD;

namespace PipelineRD.Sample;

class InitializeAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override async Task Handle(AccountRequest request)
    {
        if (!Context.FirstHandlerSuccess)
        {
            Abort("First step error.", System.Net.HttpStatusCode.BadRequest);
            return;
        }
    }
}
