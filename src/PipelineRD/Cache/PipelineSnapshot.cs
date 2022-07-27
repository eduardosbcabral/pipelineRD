namespace PipelineRD.Cache;

public class PipelineSnapshot<TContext> where TContext : BaseContext
{
    public DateTime CreatedAt { get; set; }
    public bool Success { get; set; }
    public string HandlerIdentifier { get; set; }
    public TContext Context { get; set; }

    public PipelineSnapshot()
    {

    }

    public PipelineSnapshot(bool success, string handlerIdentifier, TContext context)
    {
        CreatedAt = DateTime.UtcNow;
        Success = success;
        HandlerIdentifier = handlerIdentifier ?? string.Empty;
        Context = context;

        if (this.Context?.Result != null && this.Context?.Result.IsSuccess == false)
        {
            this.Context.Result = null;
        }
    }
}