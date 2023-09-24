using System.Net;

namespace PipelineRD;

public class HandlerResult
{
    public object Result { get; set; }
    public string LastHandler { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public List<HandlerError> Errors { get; set; }

    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    public static HandlerResult Ok(object result = null)
        => new()
        {
            StatusCode = HttpStatusCode.OK,
            Result = result
        };

    public static HandlerResult BadRequest(params HandlerError[] errors)
        => new()
        {
            Errors = errors.ToList(),
            StatusCode = HttpStatusCode.BadRequest
        };

    public static HandlerResult NoResult()
        => new()
        {
            Result = new
            {
                Message = "No result. Incomplete execution."
            },
            StatusCode = HttpStatusCode.NoContent
        };

    public static HandlerResult InternalServerError(params HandlerError[] errors)
        => new()
        {
            Errors = errors.ToList(),
            StatusCode = HttpStatusCode.InternalServerError
        };
}
