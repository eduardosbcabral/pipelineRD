using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Builders;

using System;

using static FluentValidation.AssemblyScanner;

namespace PipelineRD.Extensions
{
    public static class PipelineExtensions
    {
        public static void UsePipelineRD(this IServiceCollection services, Action<IPipelineRDBuilder> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var builder = new PipelineRDBuilder(services);
            configure(builder);
            
            if(!builder.CacheSettingsIsConfigured)
            {
                throw new ArgumentNullException("CacheSettings", "Cache settings is not configured. Use the method 'AddCacheSettings' when configuring the PipelineRD.");
            }
        }
    }
}
