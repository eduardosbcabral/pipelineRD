using PipelineRD.Tests.Steps;

using Microsoft.Extensions.DependencyInjection;

using System;

using Xunit;
using PipelineRD.Diagrams;

namespace PipelineRD.Tests
{
    public class PipelineDiagramTests
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineDiagramTests(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Fact]
        public void Should_Pipeline_Add_First_Step()
        {
            var pipeline = _serviceProvider.GetService<IPipelineDiagram<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            Assert.Equal("PipelineDiagram<ContextSample>.FirstSampleStep", pipeline.CurrentRequestStepIdentifier);
        }

        [Fact]
        public void Should_Pipeline_Proceed_To_Next_Step()
        {
            var pipeline = _serviceProvider.GetService<IPipelineDiagram<ContextSample>>();
            pipeline.AddNext<IFirstSampleStep>();
            pipeline.AddNext<ISecondSampleStep>();
            Assert.Equal("PipelineDiagram<ContextSample>.SecondSampleStep", pipeline.CurrentRequestStepIdentifier);
        }

        [Fact]
        public void Should_Pipeline_AddNext_Throw_PipelineException_After_AddFinally()
        {
            var pipeline = _serviceProvider.GetService<IPipelineDiagram<ContextSample>>();
            pipeline.AddFinally<IFirstSampleStep>();
            Assert.Throws<PipelineException>(() => pipeline.AddNext<ISecondSampleStep>());
        }
    }
}
