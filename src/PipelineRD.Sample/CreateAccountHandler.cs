using PipelineRD;

namespace PipelineRD.Sample;

class CreateAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override Task<HandlerResult> Handle(AccountRequest request)
    {
        if (!Context.SecondHandlerSuccess)
        {
            return Abort("Second step error.", System.Net.HttpStatusCode.BadRequest);
        }

        return Proceed();
    }
}

