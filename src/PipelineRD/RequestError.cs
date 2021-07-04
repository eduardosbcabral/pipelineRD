using Newtonsoft.Json;
using System;
using WebApi.Models.Response;

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

        public RequestError(string message) : this(null, message, null, null, null) { }

        public RequestError(object result) : this(null, null, null, result, null) { }

        public RequestError(Exception exception) : this(null, null, exception, null, null) { }

        public RequestError(string source, string message) : this(source, message, null, null, null) { }

        public RequestError(string source, object result) : this(source, null, null, result, null) { }

        public RequestError(string source, string message, string property) : this(source, message, null, null, property) { }

        public RequestError(ErrorItemResponse error) : this(null, error.Message, null, null, error.Property) { }

        public RequestError(string source, string message, Exception exception, object result, string property)
        {
            Exception = exception;
            Source = source;
            Message = message;
            Result = result;
            Property = property;
        }
    }
}
