using System;

namespace PipelineRD
{
    public class PipelineInitializer<TContext> : IPipelineInitializer<TContext> where TContext : BaseContext
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPipeline<TContext> Initialize() => new Pipeline<TContext>(_serviceProvider);
        public IPipeline<TContext> Initialize(string requestKey) => new Pipeline<TContext>(_serviceProvider, requestKey);

        //public IPipeline<TContext> Start(string diagramTitle, string diagramDescription) => new Pipeline<TContext>();
    }

    //public class PipelineStartingDiagram<TContext> : IPipelineInitializer<TContext> where TContext : BaseContext
    //{
    //    public IPipeline<TContext> Start() => new PipelineDiagram<TContext>();
    //    //public IPipeline<TContext> Start(string diagramTitle, string diagramDescription) => new PipelineDiagram<TContext>(diagramTitle, diagramDescription);
    //}

    public interface IPipelineInitializer<TContext> where TContext : BaseContext
    {
        IPipeline<TContext> Initialize();
        IPipeline<TContext> Initialize(string requestKey);

        //IPipeline<TContext> Start(string diagramTitle, string diagramDescription);
    }
}