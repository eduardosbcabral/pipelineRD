using Newtonsoft.Json;
using System;

namespace PipelineRD
{
    public class RequestError
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Exception Exception { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Property { get; set; }
        
        public RequestError() { }

        public RequestError(string message)
        {
            Message = message;
        }

        public RequestError(string source, string message)
        {
            Source = source;
            Message = message;
        }

        public RequestError(Exception exception)
        {
            Exception = exception;
        }

        public RequestError(string source, string property, string message, object result, Exception exception)
        {
            Source = source;
            Property = property;
            Message = message;
            Result = result; 
            Exception = exception;
        }
    }
}
