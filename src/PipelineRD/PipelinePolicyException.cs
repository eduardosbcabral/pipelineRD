using System;

namespace PipelineRD
{
    public class PipelinePolicyException : Exception
    {
        public RequestStepResult Result { get; private set; }

        public PipelinePolicyException(RequestStepResult result)
        {
            this.Result = result;
        }
    }
}
