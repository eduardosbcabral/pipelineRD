namespace PipelineRD
{
    public interface IPipelineDiagram<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
    }
}
