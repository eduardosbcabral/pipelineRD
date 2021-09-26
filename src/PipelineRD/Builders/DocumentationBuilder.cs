using DiagramBuilder.Html;

using PipelineRD.Builders;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PipelineRD
{
    public class DocumentationBuilder : IDocumentationBuilder
    {
        private readonly IList<HtmlCustomDiagram> _diagrams;
        private readonly IEnumerable<TypeInfo> _types;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _applicationName;

        private bool isAlreadySet;

        public DocumentationBuilder()
        {
            _diagrams = new List<HtmlCustomDiagram>();
        }

        public DocumentationBuilder(string applicationName, IEnumerable<TypeInfo> types, IServiceProvider serviceProvider) : this()
        {
            _types = types;
            _serviceProvider = serviceProvider;
            _applicationName = applicationName;
        }

        public void AddDiagram(HtmlCustomDiagram customDiagram)
            => _diagrams.Add(customDiagram);

        public void UsePath(string path)
        {
            if (isAlreadySet) return;

            Build();
            new HtmlBuilder().BuildDocumentation(path, _applicationName, _diagrams.ToArray());
            isAlreadySet = true;
        }

        private void Build()
        {
            var pipelineBuilders = from type in _types
                where !type.IsAbstract && !type.IsGenericTypeDefinition
                let interfaces = type.GetInterfaces()
                let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBuilder<>))
                let matchingInterface = genericInterfaces.FirstOrDefault()
                where matchingInterface != null
                select new { Interface = matchingInterface, Type = type };

            foreach (var builder in pipelineBuilders)
            {
                var initializerDiagramType = typeof(PipelineInitializerDiagram<>);
                var builderGenericType = builder.Interface.GetGenericArguments().FirstOrDefault();
                var initializerDiagramWithContext = initializerDiagramType.MakeGenericType(builderGenericType);
                var args = new object[]
                {
                    Activator.CreateInstance(initializerDiagramWithContext, new object[] { _serviceProvider, this })
                };
                var builderWithDiagramInstance = Activator.CreateInstance(builder.Type, args);
                var builderMethods = builder.Type.GetMethods();

                foreach (var method in builderMethods.Where(m => m.ReturnType == typeof(Task<RequestStepResult>)))
                {
                    var parameter = method.GetParameters().FirstOrDefault();
                    var requestInstance = Activator.CreateInstance(parameter.ParameterType, null);
                    method.Invoke(builderWithDiagramInstance, new object[] { requestInstance });
                }
            }
        }
    }
}