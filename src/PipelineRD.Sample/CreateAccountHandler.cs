using PipelineRD;

namespace PipelineRD.Sample;

class CreateAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override void Handle(AccountRequest request)
    {
        if (!Context.SecondHandlerSuccess)
        {
            Abort("Second step error.", System.Net.HttpStatusCode.BadRequest);
            return;
        }
    }
}

