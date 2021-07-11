using PipelineRD.Diagrams;

using System;

namespace PipelineRD
{
    public class PipelineInitializer<TContext> : IPipelineInitializer<TContext> where TContext : BaseContext
    {
        private readonly IPipeline<TContext> _pipeline;

        public PipelineInitializer(IPipeline<TContext> pipeline)
        {
            _pipeline = pipeline;
        }

        public IPipeline<TContext> Initialize() => _pipeline;
        public IPipeline<TContext> Initialize(string requestKey)
        {
            _pipeline.SetRequestKey(requestKey);
            return _pipeline;
        }
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
    }
}