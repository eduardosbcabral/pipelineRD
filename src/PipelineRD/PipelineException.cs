namespace PipelineRD;

public class PipelineException : Exception
{
    public HandlerResult Result { get; private set; }

    public PipelineException(HandlerResult result)
    {
        this.Result = result;
    }

    public PipelineException(string message) : base($"[PipelineRD] {message}") { }
}
