using PipelineRD;

namespace PipelineRD.Sample;

class CreateAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override Task Handle(AccountRequest request)
    {
        if (!Context.SecondHandlerSuccess)
        {
            Abort("Second step error.", System.Net.HttpStatusCode.BadRequest);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}

