namespace PipelineRD;

public abstract class RecoveryHandler<TContext, TRequest> where TContext : BaseContext
{
    public TContext Context { get; private set; }

    public abstract void Handle(TRequest request);

    public void DefineContext(TContext context)
        => Context = context;
}
