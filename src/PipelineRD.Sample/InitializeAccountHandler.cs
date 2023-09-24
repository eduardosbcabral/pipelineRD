using PipelineRD;

namespace PipelineRD.Sample;

class InitializeAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override Task Handle(AccountRequest request)
    {
        if (!Context.FirstHandlerSuccess)
        {
            Abort("First step error.", System.Net.HttpStatusCode.BadRequest);
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
}
