using System.Text.Json.Serialization;

namespace PipelineRD;

public class HandlerError
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Source { get; set; }

    public HandlerError(string message)
    {
        Message = message;
    }

    public HandlerError(string source, string message)
    {
        Source = source;
        Message = message;
    }
}