namespace PipelineRD
{
    public interface IPipelineBuilder<TContext> where TContext : BaseContext
    {
        IPipelineInitializer<TContext> Pipeline { get; }
    }
}