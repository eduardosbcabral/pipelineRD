using System.Collections.Generic;

namespace PipelineRD.Tests
{
    public class ContextSample : BaseContext
    {
        public bool ValidFirst { get; set; } = true;
        public bool ValidSecond { get; set; } = true;

        public bool FirstWasExecuted { get; set; } = false;
        public bool SecondWasExecuted { get; set; } = false;
        public bool ThirdWasExecuted { get; set; } = false;
        public bool RollbackWasExecuted { get; set; } = false;

        public bool FirstRollbackWasExecuted { get; set; } = false;
        public bool SecondRollbackWasExecuted { get; set; } = false;

        public int SecondWasExecutedCount { get; set; }

        public IEnumerable<string> Values { get; set; }

        public ContextSample()
        {
        }
    }
}
