using Microsoft.Extensions.DependencyInjection;
using PipelineRD.Cache;
using PipelineRD.Extensions;

namespace PipelineRD.Validation.Tests
{
    class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.UsePipelineRD(x =>
            {
                x.SetupCache(new PipelineRDCacheSettings());
                x.SetupPipelineServices(x =>
                {
                    x.InjectAll();
                    x.InjectRequestValidators();
                });
            });
        }
    }
}
