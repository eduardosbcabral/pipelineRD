using System.Text.Json.Serialization;

namespace PipelineRD
{
    [JsonConverter(typeof(ContextConverter))]
    public abstract class BaseContext
    {
        public string Id { get; set; }
        public object PipelineRequest { get; set; }
        public RequestStepResult Response { get; set; }

        public BaseContext()
        {
            Id = ToString();
        }

        public void SetRequest(object request)
            => PipelineRequest = request;

        public TRequest Request<TRequest>()
            => (TRequest)PipelineRequest;
    }
}
