using PipelineRD;

namespace PipelineRD.Sample;

class FinishAccountHandler : Handler<AccountContext, AccountRequest>
{
    public override Task<HandlerResult> Handle(AccountRequest request)
    {
        if (!Context.ThirdHandlerSuccess)
        {
            return Abort("Third step error.", System.Net.HttpStatusCode.BadRequest);
        }

        return Finish(new { Message = "Success", Id = 1 }, System.Net.HttpStatusCode.Created);
    }
}

