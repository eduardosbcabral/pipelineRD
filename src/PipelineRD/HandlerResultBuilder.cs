using System.Net;
using System.Runtime.InteropServices;

namespace PipelineRD;

public static class HandlerResultBuilder
{

    public static HandlerResult CreateDefault([Optional] HandlerResult handlerResult)
    {
        return handlerResult ?? new();
    }

    public static HandlerResult WithResult(this HandlerResult handlerResult, object result)
    {
        handlerResult.Result = result;
        return handlerResult;
    }

    public static HandlerResult WithError(this HandlerResult handlerResult, HandlerError error)
    {
        if(handlerResult.Errors == null)
        {
            handlerResult.Errors = new();
        }

        handlerResult.Errors.Add(error);

        return handlerResult;
    }

    public static HandlerResult WithErrors(this HandlerResult handlerResult, IEnumerable<HandlerError> errors)
    {
        if (handlerResult.Errors == null)
        {
            handlerResult.Errors = new();
        }

        handlerResult.Errors.AddRange(errors);

        return handlerResult;
    }

    public static HandlerResult WithStatusCode(this HandlerResult handlerResult, HttpStatusCode httpStatusCode)
    {
        handlerResult.StatusCode = httpStatusCode;
        return handlerResult;
    }

    public static HandlerResult WithLastHandler(this HandlerResult handlerResult, string lastHandler)
    {
        handlerResult.LastHandler = lastHandler;
        return handlerResult;
    }
}
