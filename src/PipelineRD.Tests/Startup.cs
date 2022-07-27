using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Cache;
using PipelineRD.Extensions;

namespace PipelineRD.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.UsePipelineRD(x =>
            {
                var cacheSettings = new PipelineRDCacheSettings();

                x.UseCache(cacheSettings);
                x.AddPipelineServices(x => x.InjectAll());
            });
        }
    }
}
