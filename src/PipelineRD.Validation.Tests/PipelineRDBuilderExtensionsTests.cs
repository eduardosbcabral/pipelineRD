using FluentValidation;

using Microsoft.Extensions.DependencyInjection;
using PipelineRD.Cache;
using PipelineRD.Extensions;

using System.Linq;

using Xunit;

namespace PipelineRD.Validation.Tests
{
    public class PipelineRDBuilderExtensionsTests
    {
        [Fact]
        public void Should_UsePipelineRD_AddPipelineServices_And_Check_If_IValidatorRequest_Is_Singleton()
        {
            var services = new ServiceCollection();

            services.UsePipelineRD(x =>
            {
                x.SetupCache(new PipelineRDCacheSettings());
                x.SetupPipelineServices(x => x.InjectRequestValidators());
            });

            var provider = services.BuildServiceProvider();

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(IValidator<PipelineRDRequestTest>));

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
        }

        class PipelineRDRequestTest { }

        class PipelineRDRequestTestValidator : AbstractValidator<PipelineRDRequestTest> { }
    }
}
