using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Diagrams;
using PipelineRD.Extensions;
using PipelineRD.Settings;
using PipelineRD.Tests.Conditions;

namespace PipelineRD.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices(x => x.InjectAll());
            });

            services.AddSingleton<ISampleCondition, SampleCondition>();
        }
    }
}
