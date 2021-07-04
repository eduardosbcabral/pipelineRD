namespace PipelineRD.Diagrams
{
    public interface IPipelineDiagram<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
    }
}
