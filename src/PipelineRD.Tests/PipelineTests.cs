using PipelineRD.Tests.Request;
using PipelineRD.Tests.Steps;

using Microsoft.Extensions.DependencyInjection;

using System;

using Xunit;
using PipelineRD.Tests.Conditions;
using PipelineRD.Extensions;
using PipelineRD.Tests.Validators;
using System.Linq;
using Polly;
using Nancy;

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
        public void Should_Pipeline_Add_First_Step()
        {
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            Assert.Equal("Pipeline<ContextSample>.FirstSampleStep", pipeline.CurrentRequestStepIdentifier);
        }

        [Fact]
        public void Should_Pipeline_Proceed_To_Next_Step()
        {
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            Assert.Equal("Pipeline<ContextSample>.SecondSampleStep", pipeline.CurrentRequestStepIdentifier);
        }

        [Fact]
        public void Should_Pipeline_AddNext_Throw_PipelineException_After_AddFinally()
        {
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddFinally<IFirstSampleStep>();
            Assert.Throws<PipelineException>(() => pipeline.AddNext<ISecondSampleStep>());
        }

        [Fact]
        public void Should_Pipeline_Validate_Request()
        {
            var request = new SampleRequest() { ValidModel = false };
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddValidator<SampleRequest>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();
            pipeline.Execute(request);

            var result = pipeline.Execute(request);

            Assert.Equal(400, result.StatusCode);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Should_Pipeline_Validate_Request_Using_Validator_Implementation()
        {
            var request = new SampleRequest() { ValidModel = false };
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            var validator = new SampleRequestValidator();
            pipeline.AddValidator(validator);
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();
            pipeline.Execute(request);

            var result = pipeline.Execute(request);

            Assert.Equal(400, result.StatusCode);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Should_Pipeline_Finish_With_Status_200()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();

            var result = pipeline.Execute(request);

            Assert.Equal(200, result.StatusCode);
            Assert.True(pipeline.Context.FirstWasExecuted);
            Assert.True(pipeline.Context.SecondWasExecuted);
            Assert.True(pipeline.Context.ThirdWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Abort_With_Status_400()
        {
            var request = new SampleRequest() { ValidFirst = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            var result = pipeline.Execute(request);

            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void Should_Pipeline_Not_Execute_Step_When_Condition_Is_Not_Met()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>()
                .When(x => x.ValidSecond);
            pipeline.AddNext<IThirdSampleStep>();

            var result = pipeline.Execute(request);

            Assert.False(pipeline.Context.SecondWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Not_Execute_Step_When_Condition_IoC_Is_Not_Met()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>()
                .When<ISampleCondition>();
            pipeline.AddNext<IThirdSampleStep>();

            pipeline.Context.ValidSecond = false;

            pipeline.Execute(request);

            Assert.False(pipeline.Context.SecondWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Use_Recovery_By_Hash_Per_Default()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();
            pipeline.Execute(request);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot>(request.GenerateHash(pipeline.Identifier)).Result;

            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Should_Pipeline_Use_Recovery_By_Hash()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.EnableRecoveryRequestByHash();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();
            pipeline.Execute(request);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot>(request.GenerateHash(pipeline.Identifier)).Result;

            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Should_Pipeline_Not_Use_Recovery_By_Hash()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.DisableRecoveryRequestByHash();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();
            pipeline.Execute(request);

            var cacheProvider = _serviceProvider.GetService<ICacheProvider>();
            var snapshot = cacheProvider.Get<PipelineSnapshot>(request.GenerateHash(pipeline.Identifier)).Result;

            Assert.Null(snapshot);
        }

        [Fact]
        public void Should_Second_Pipeline_Use_Recovery_By_Existent_Snapshot()
        {
            var idempotencyKey = "key";
            var request = new SampleRequest() { ValidSecond = false };

            var pipelineOne = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipelineOne.AddNext<IFirstSampleStep>();
            pipelineOne.AddNext<ISecondSampleStep>();

            pipelineOne.Execute(request, idempotencyKey);

            var pipelineTwo = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipelineTwo.AddNext<IFirstSampleStep>();
            pipelineTwo.AddNext<ISecondSampleStep>();
            pipelineTwo.AddNext<IThirdSampleStep>();

            request.ValidSecond = true;

            var pipelineTwoResult = pipelineTwo.Execute(request, idempotencyKey);

            Assert.Equal(200, pipelineTwoResult.StatusCode);
        }

        [Fact]
        public void Should_Pipeline_Add_Rollback_Step()
        {
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddRollback<IFirstSampleRollbackStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddRollback<ISecondSampleRollbackStep>();

            var firstStep = _serviceProvider.GetService<IFirstSampleStep>();
            var secondStep = _serviceProvider.GetService<ISecondSampleRollbackStep>();

            Assert.Equal(0, firstStep.RollbackIndex);
            Assert.Equal(1, secondStep.RollbackIndex);
        }

        [Fact]
        public void Should_Pipeline_Execute_All_Rollback_Steps()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddRollback<IFirstSampleRollbackStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddRollback<ISecondSampleRollbackStep>();
            pipeline.AddNext<IRollbackSampleStep>();

            var pipelineResult = pipeline.Execute(request);

            Assert.Equal(201, pipelineResult.StatusCode);
            Assert.True(pipeline.Context.FirstRollbackWasExecuted);
            Assert.True(pipeline.Context.SecondRollbackWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Execute_Until_Certain_Index_Rollback_Steps()
        {
            var request = new SampleRequest();
            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddRollback<IFirstSampleRollbackStep>();
            pipeline.AddNext<ISecondSampleStep>();
            pipeline.AddNext<IRollbackSampleStep>();
            pipeline.AddNext<IThirdSampleStep>();
            pipeline.AddRollback<ISecondSampleRollbackStep>();

            var pipelineResult = pipeline.Execute(request);

            Assert.Equal(201, pipelineResult.StatusCode);
            Assert.True(pipeline.Context.FirstRollbackWasExecuted);
            Assert.False(pipeline.Context.SecondRollbackWasExecuted);
        }

        [Fact]
        public void Should_Pipeline_Set_Step_Policy()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            var policy = Policy
                .HandleResult<RequestStepResult>(x => x.StatusCode == (int)HttpStatusCode.BadRequest)
                .Retry(3);
            pipeline.AddNext<IFirstSampleStep>()
                .WithPolicy(policy);

            var currentStep = pipeline.Steps.FirstOrDefault();
            Assert.NotNull(currentStep.Policy);
        }

        [Fact]
        public void Should_Pipeline_Use_Policy_Retry()
        {
            var request = new SampleRequest() { ValidSecond = false };

            var pipeline = _serviceProvider.GetService<IPipeline<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();

            var policy = Policy
                .HandleResult<RequestStepResult>(x => x.StatusCode == (int)HttpStatusCode.BadRequest)
                .Retry(3);

            pipeline.AddNext<ISecondSampleStep>()
                .WithPolicy(policy);
            pipeline.AddNext<IThirdSampleStep>();

            pipeline.Context.ValidSecond = false;

            pipeline.Execute(request);

            // Four because it will execute once and retry 3 more times.
            Assert.Equal(4, pipeline.Context.SecondWasExecutedCount);
        }
    }
}
