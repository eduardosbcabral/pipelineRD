using FluentValidation;

using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Extensions;
using PipelineRD.Settings;

using System;
using System.Linq;

using Xunit;

namespace PipelineRD.Tests.Builders
{
    public class IPipelineRDBuilderTests
    {
        [Fact]
        public void Should_UsePipelineRD_And_UseCacheInMemory()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
            });

            var provider = services.BuildServiceProvider();

            var cacheSettings = provider.GetService<ICacheSettings>();
            var cacheProvider = provider.GetService<ICacheProvider>();
            Assert.NotNull(cacheSettings);
            Assert.NotNull(cacheProvider);
            Assert.IsType<MemoryCacheSettings>(cacheSettings);
        }

        [Fact]
        public void Should_UsePipelineRD_And_UseCacheInRedis()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInRedis(new RedisCacheSettings());
            });

            var provider = services.BuildServiceProvider();

            var cacheSettings = provider.GetService<ICacheSettings>();
            var cacheProvider = provider.GetService<ICacheProvider>();
            Assert.NotNull(cacheSettings);
            Assert.NotNull(cacheProvider);
            Assert.IsType<RedisCacheSettings>(cacheSettings);
        }

        [Fact]
        public void Should_UsePipelineRD_And_AddPipelineServices()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
            });
           
            var provider = services.BuildServiceProvider();

            var context = provider.GetService<PipelineRDContextTest>();
            var step = provider.GetService<IPipelineRDTestStep>();
            var rollbackStep = provider.GetService<IPipelineRDTestRollbackStep>();
            var pipeline = provider.GetService<IPipeline<PipelineRDContextTest>>();
            var validator = provider.GetService<IValidator<PipelineRDRequestTest>>();
            var initializer = provider.GetService<IPipelineInitializer<PipelineRDContextTest>>();
            var builder = provider.GetService<IPipelineBuilder<PipelineRDContextTest>>();
            Assert.NotNull(context);
            Assert.NotNull(step);
            Assert.NotNull(rollbackStep);
            Assert.NotNull(pipeline);
            Assert.NotNull(validator);
            Assert.NotNull(initializer);
            Assert.NotNull(builder);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_Context_Is_Scoped()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
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
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IPipelineRDTestStep));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_IPipeline_Is_Transient()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IPipeline<>));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_IValidatorRequest_Is_Singleton()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IValidator<PipelineRDRequestTest>));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_IPipelineInitializer_Is_Singleton()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IPipelineInitializer<>));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_IPipelineBuilder_Is_Transient()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IPipelineBuilder<PipelineRDContextTest>));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
        }

        [Fact]
        public void Should_UsePipelineRD_Without_ConfiguringCache_Throws_Exception()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.UsePipelineRD(x => { }));
        }
    }

    class PipelineRDTestStep : RequestStep<PipelineRDContextTest>, IPipelineRDTestStep
    {
        public override RequestStepResult HandleRequest() => Next();
    }

    class PipelineRDRTestRollbackStep : RollbackRequestStep<PipelineRDContextTest>, IPipelineRDTestRollbackStep
    {
        public override void HandleRollback() { }
    }

    interface IPipelineRDTestStep : IRequestStep<PipelineRDContextTest>
    { }

    interface IPipelineRDTestRollbackStep : IRollbackRequestStep<PipelineRDContextTest>
    { }

    class PipelineRDContextTest : BaseContext
    { }

    class PipelineRDRequestTest : IPipelineRequest
    { }

    class PipelineRDRequestTestValidator : AbstractValidator<PipelineRDRequestTest>
    { }

    class PipelineRDBuilderTest : IPipelineBuilder<PipelineRDContextTest>
    {
        public IPipelineInitializer<PipelineRDContextTest> Pipeline { get; }

        public PipelineRDBuilderTest(IPipelineInitializer<PipelineRDContextTest> pipeline) => Pipeline = pipeline;
    }
}
