using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.Reflection;

namespace PipelineRD.Builders
{
    public interface IPipelineServicesBuilder
    {
        void InjectContexts();
        void InjectSteps();
        void InjectPipelines();
        void InjectPipelineInitializers();
        void InjectPipelineBuilders();
        void InjectAll();

        IEnumerable<TypeInfo> Types { get; }
        IServiceCollection Services { get; }
        bool ValidatorsAlreadySet { get; set; }
    }
}
