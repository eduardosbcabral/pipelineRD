using PipelineRD.Diagrams;

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
    }

    internal class PipelineInitializerDiagram<TContext> : IPipelineInitializer<TContext> where TContext : BaseContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DocumentationBuilder _documentationBuilder;

        public PipelineInitializerDiagram(IServiceProvider serviceProvider, DocumentationBuilder documentationBuilder)
        {
            _serviceProvider = serviceProvider;
            _documentationBuilder = documentationBuilder;

        }
        public IPipeline<TContext> Initialize() => new PipelineDiagram<TContext>(_serviceProvider, _documentationBuilder);
        public IPipeline<TContext> Initialize(string requestKey) => new PipelineDiagram<TContext>(_serviceProvider, _documentationBuilder);
    }

    public interface IPipelineInitializer<TContext> where TContext : BaseContext
    {
        IPipeline<TContext> Initialize();
        IPipeline<TContext> Initialize(string requestKey);

        //IPipeline<TContext> Start(string diagramTitle, string diagramDescription);
    }
}