using System.Net;

namespace PipelineRD;

public class HandlerResult
{
    public object Result { get; set; }
    public string LastHandler { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public List<HandlerError> Errors { get; set; }

    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    public static HandlerResult OK(object result = null)
        => new()
        {
            StatusCode = HttpStatusCode.OK,
            Result = result
        };

    public static HandlerResult BadRequest(object result = null)
        => new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Result = result
        };

    public static HandlerResult NoResult()
        => new()
        {
            Result = new
            {
                Message = "No result."
            },
            StatusCode = 0
        };
}
