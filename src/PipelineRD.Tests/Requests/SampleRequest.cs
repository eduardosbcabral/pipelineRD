using System;

namespace PipelineRD.Tests.Request
{
    public class SampleRequest : IPipelineRequest
    {
        public Guid Guid { get; set; } = Guid.NewGuid();

        public bool ValidFirst { get; set; } = true;
        public bool ValidSecond { get; set; } = true;

        public bool ValidModel { get; set; }
    }
}
