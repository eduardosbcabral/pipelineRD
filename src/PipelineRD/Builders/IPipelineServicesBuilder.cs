using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.Reflection;

namespace PipelineRD.Builders
{
    using Enums;

    public interface IPipelineServicesBuilder
    {
        void InjectContexts(InjectionLifetime? lifetime = null);
        void InjectSteps(InjectionLifetime? lifetime = null);
        void InjectPipelines(InjectionLifetime? lifetime = null);
        void InjectPipelineInitializers(InjectionLifetime? lifetime = null);
        void InjectPipelineBuilders(InjectionLifetime? lifetime = null);
        void InjectAll(InjectionLifetime? lifetime = null);

        IEnumerable<TypeInfo> Types { get; }
        IServiceCollection Services { get; }
        bool ValidatorsAlreadySet { get; set; }
    }
}
