using System;

namespace PipelineRD
{
    public class PipelineSnapshot
    {
        public DateTime CreatedAt { get; set; }
        public bool Success { get; private set; }
        public string LastRequestIdentifier { get; private set; }
        public BaseContext Context { get; set; }

        public PipelineSnapshot()
        {

        }

        public PipelineSnapshot(bool success, string lastRequestHandlerId, BaseContext context)
        {
            this.CreatedAt = DateTime.UtcNow;
            this.Success = success;
            this.LastRequestIdentifier = lastRequestHandlerId;
            this.Context = context;

            if (this.Context?.Response != null && this.Context.Response.IsSuccess() == false)
            {
                this.Context.Response = null;
            }
        }
    }
}
