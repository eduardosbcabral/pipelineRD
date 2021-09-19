using System.Text.Json.Serialization;

namespace PipelineRD
{
    [JsonConverter(typeof(ContextConverter))]
    public abstract class BaseContext
    {
        public string Id { get; set; }
        public IPipelineRequest PipelineRequest { get; set; }
        public RequestStepResult Response { get; set; }

        public BaseContext()
        {
            Id = ToString();
        }

        public void SetRequest(IPipelineRequest request)
            => PipelineRequest = request;

        public TRequest Request<TRequest>() where TRequest : IPipelineRequest
            => (TRequest)PipelineRequest;
    }
}
