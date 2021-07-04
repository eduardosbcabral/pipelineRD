using System.Text.Json.Serialization;

namespace PipelineRD
{
    [JsonConverter(typeof(ContextConverter))]
    public abstract class BaseContext
    {
        public string Id { get; set; }
        public IPipelineRequest Request { get; set; }
        public RequestStepResult Response { get; set; }

        public BaseContext()
        {
            Id = ToString();
        }
    }
}
