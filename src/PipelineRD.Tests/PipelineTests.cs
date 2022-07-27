using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Cache;
using PipelineRD.Tests.Handlers;
using PipelineRD.Tests.Request;

using Polly;

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Xunit;

namespace PipelineRD.Tests
{
    public class PipelineTests
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineTests(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Fact]
        public void Should_Pipeline_Add_First_Handler()
        {
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();

            var handler = pipeline.Handlers.LastOrDefault();
            Assert.Contains("FirstSampleHandler", handler.Identifier);
        }

        [Fact]
        public void Should_Pipeline_Proceed_To_Next_Handler()
        {
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>();

            var handler = pipeline.Handlers.LastOrDefault();
            Assert.Contains("SecondSampleHandler", handler.Identifier);
        }

        [Fact]
        public async Task Should_Pipeline_Finish_With_Status_200()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>();
            pipeline.WithHandler<ThirdSampleHandler>();

            var result = pipeline.Execute(request);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.True(pipeline.Context.FirstWasExecuted);
            Assert.True(pipeline.Context.SecondWasExecuted);
            Assert.True(pipeline.Context.ThirdWasExecuted);
        }

        [Fact]
        public async Task Should_Pipeline_Abort_With_Status_400()
        {
            var request = new SampleRequest() { ValidFirst = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();
            var result = pipeline.Execute(request);

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void Should_Pipeline_Not_Execute_Handler_When_Condition_Is_Not_Met()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>()
                .When(x => x.ValidSecond);
            pipeline.WithHandler<ThirdSampleHandler>();

            var result = pipeline.Execute(request);

            Assert.False(pipeline.Context.SecondWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Not_Execute_Handler_When_Condition_IoC_Is_Not_Met()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>()
                .When(x => x.ValidSecond == true);
            pipeline.WithHandler<ThirdSampleHandler>();

            pipeline.Context.ValidSecond = false;

            pipeline.Execute(request);

            Assert.False(pipeline.Context.SecondWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Use_Recovery_By_Hash_Per_Default()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>();
            pipeline.WithHandler<ThirdSampleHandler>();
            pipeline.Execute(request);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot<ContextSample>>(pipeline.GetRequestHash(request, "123"));

            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Should_Pipeline_Use_Cache_By_Request_Hash()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.EnableCache();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>();
            pipeline.WithHandler<ThirdSampleHandler>();
            pipeline.Execute(request);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot<ContextSample>>(pipeline.GetRequestHash(request, string.Empty));

            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Should_Pipeline_Use_Cache_By_Idempotency_Hash()
        {
            var request = new SampleRequest();
            var idempotencyKey = "123";
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.EnableCache();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>();
            pipeline.WithHandler<ThirdSampleHandler>();
            pipeline.Execute(request, idempotencyKey);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot<ContextSample>>(pipeline.GetRequestHash(request, idempotencyKey));

            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Should_Pipeline_Not_Use_Recovery_By_Hash()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.DisableCache();
            pipeline.WithHandler<FirstSampleHandler>();
            pipeline.WithHandler<SecondSampleHandler>();
            pipeline.WithHandler<ThirdSampleHandler>();
            pipeline.Execute(request);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot<ContextSample>>(pipeline.GetRequestHash(request, "123456"));

            Assert.Null(snapshot);
        }

        [Fact]
        public async Task Should_Second_Pipeline_Use_Cache_By_Existent_Snapshot()
        {
            var idempotencyKey = "key";
            var request = new SampleRequest() { ValidSecond = false };

            var pipelineOne = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipelineOne.EnableCache();
            pipelineOne.WithHandler<FirstSampleHandler>();
            pipelineOne.WithHandler<SecondSampleHandler>();

            pipelineOne.Execute(request, idempotencyKey);

            var pipelineTwo = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipelineTwo.EnableCache();
            pipelineTwo.WithHandler<FirstSampleHandler>();
            pipelineTwo.WithHandler<SecondSampleHandler>();
            pipelineTwo.WithHandler<ThirdSampleHandler>();

            request.ValidSecond = true;

            var pipelineTwoResult = pipelineTwo.Execute(request, idempotencyKey);

            Assert.Equal(HttpStatusCode.OK, pipelineTwoResult.StatusCode);
        }

        [Fact]
        public void Should_Pipeline_Set_Handler_Policy()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            var policy = Policy
                .HandleResult<HandlerResult>(x => x.StatusCode == HttpStatusCode.BadRequest)
                .Retry(3);
            pipeline.WithHandler<FirstSampleHandler>()
                .WithPolicy(policy);

            var currentHandler = pipeline.Handlers.FirstOrDefault();
            Assert.NotNull(currentHandler.Policy);
        }

        [Fact]
        public void Should_Pipeline_Use_Policy_Retry()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample, SampleRequest>>();
            pipeline.WithHandler<FirstSampleHandler>();

            var policy = Policy
                .HandleResult<HandlerResult>(x => x.StatusCode == HttpStatusCode.BadRequest)
                .Retry(3);

            pipeline.WithHandler<SecondSampleHandler>()
                .WithPolicy(policy);
            pipeline.WithHandler<ThirdSampleHandler>();

            pipeline.Context.ValidSecond = false;

            pipeline.Execute(request);

            // Four because it will execute once and retry 3 more times.
            Assert.Equal(4, pipeline.Context.SecondWasExecutedCount);
        }

        [Fact]
        public void Should_test_execute_successfully()
        {
            var request = new SampleRequest()
            {
                ValidFirst = true
            };

            var context = new ContextSample();

            var handler = new FirstSampleHandler();
            handler.DefineContext(context);

            handler.Handle(request);

            var result = handler.Result;

            Assert.Null(result);
            Assert.True(context.FirstWasExecuted);
        }
    }
}
