using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Extensions;
using PipelineRD.Tests.Conditions;

namespace PipelineRD.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.SetupPipelineR();

            // Injecting service for the interface IDistributedCache
            services.AddDistributedMemoryCache();

            var cacheSettings = new CacheSettings();

            services.AddSingleton(cacheSettings);

            var provider = services.BuildServiceProvider();

            var distributedCache = provider.GetService<IDistributedCache>();

            services.AddSingleton<ICacheProvider>(new CacheProvider(cacheSettings, distributedCache));
            services.AddSingleton<ISampleCondition, SampleCondition>();
        }
    }
}
