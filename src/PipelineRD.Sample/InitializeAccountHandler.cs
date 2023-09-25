using PipelineRD;

namespace PipelineRD.Sample;

class InitializeAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override Task<HandlerResult> Handle(AccountRequest request)
    {
        if (!Context.FirstHandlerSuccess)
        {
            return Abort("First step error.", System.Net.HttpStatusCode.BadRequest);
        }

        return Proceed();
    }
}
