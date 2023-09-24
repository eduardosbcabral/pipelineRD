using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Cache;
using PipelineRD.Extensions;

namespace PipelineRD.Tests
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.UsePipelineRD(x =>
            {
                x.SetupCache(new PipelineRDCacheSettings());
                x.SetupPipelineServices(x => x.InjectAll());
            });
        }
    }
}
