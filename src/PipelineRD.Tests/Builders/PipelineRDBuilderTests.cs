
using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Cache;
using PipelineRD.Extensions;

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PipelineRD.Tests.Builders
{
    public class PipelineRDBuilderTests
    {
        [Fact]
        public void Should_UsePipelineRD_And_UseCacheInMemory()
        {
            var services = new ServiceCollection();

            services.AddDistributedMemoryCache();

            services.UsePipelineRD(x =>
            {
                x.UseCache(new PipelineRDCacheSettings());
            });

            var provider = services.BuildServiceProvider();

            var cacheSettings = provider.GetService<IPipelineRDCacheSettings>();
            var cacheProvider = provider.GetService<ICacheProvider>();
            Assert.NotNull(cacheSettings);
            Assert.NotNull(cacheProvider);
            Assert.IsType<PipelineRDCacheSettings>(cacheSettings);
        }

        [Fact]
        public void Should_UsePipelineRD_And_AddPipelineServices()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCache(new PipelineRDCacheSettings());
                x.AddPipelineServices(x => x.InjectAll());
            });

            var provider = services.BuildServiceProvider();

            var context = provider.GetService<PipelineRDContextTest>();
            var step = provider.GetService<PipelineRDTestStep>();
            var pipeline = provider.GetService<IPipeline<PipelineRDContextTest, PipelineRDRequestTest>>();

            Assert.NotNull(context);
            Assert.NotNull(step);
            Assert.NotNull(pipeline);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_Context_Is_Scoped()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCache(new PipelineRDCacheSettings());
                x.AddPipelineServices(x => x.InjectContexts());
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(PipelineRDContextTest));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_Step_Is_Scoped()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCache(new PipelineRDCacheSettings());
                x.AddPipelineServices(x => x.InjectHandlers());
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(PipelineRDTestStep));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_IPipeline_Is_Scoped()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCache(new PipelineRDCacheSettings());
                x.AddPipelineServices(x => x.InjectPipelines());
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IPipeline<,>));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
        }
    }

    class PipelineRDTestStep : Handler<PipelineRDContextTest, PipelineRDRequestTest>
    {
        public override async Task Handle(PipelineRDRequestTest request)
        {

        }
    }

    class PipelineRDContextTest : BaseContext { public bool Valid { get; set; } }

    class PipelineRDRequestTest { }
}
