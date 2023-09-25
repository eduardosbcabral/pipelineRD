using FluentValidation;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using System.Net;

namespace PipelineRD.Validation.Tests
{
    public class PipelineRDExtensionsTests
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineRDExtensionsTests(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Fact]
        public async Task Should_Pipeline_Validate_Request()
        {
            var request = new SampleRequest() { ValidModel = false };
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleStep>();
            pipeline.WithHandler<SecondSampleStep>();
            pipeline.WithHandler<ThirdSampleStep>();

            var result = await pipeline.ExecuteWithValidation(request);

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task Should_Pipeline_Validate_Request_Using_Validator_Implementation()
        {
            var request = new SampleRequest() { ValidModel = false };
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            var validator = new SampleRequestValidator();
            pipeline.WithHandler<FirstSampleStep>();
            pipeline.WithHandler<SecondSampleStep>();
            pipeline.WithHandler<ThirdSampleStep>();

            var result = await pipeline.ExecuteWithValidation(request, validator);

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Single(result.Errors);
        }
    }

    public class SampleRequest
    {
        public Guid Guid { get; set; } = Guid.NewGuid();

        public bool ValidFirst { get; set; } = true;
        public bool ValidSecond { get; set; } = true;

        public bool ValidModel { get; set; }
    }

    public class ContextSample : BaseContext
    {
        public bool ValidFirst { get; set; } = true;

        public ContextSample()
        {
        }
    }

    public class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(x => x.ValidModel)
                .Equal(true);
        }
    }

    public class FirstSampleStep : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest request)
        {
            return Proceed();
        }
    }

    public class SecondSampleStep : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest request)
        {
            return Proceed();
        }
    }

    public class ThirdSampleStep : Handler<ContextSample, SampleRequest>
    {
        public override Task<HandlerResult> Handle(SampleRequest request)
        {
            return this.Finish(200);
        }
    }
}
