using PipelineRD;

namespace PipelineRD.Sample;

class FinishAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override async Task Handle(AccountRequest request)
    {
        if (!Context.ThirdHandlerSuccess)
        {
            Abort("Third step error.", System.Net.HttpStatusCode.BadRequest);
            return;
        }

        Finish(new { Message = "Success", Id = 1 }, System.Net.HttpStatusCode.Created);
    }
}

