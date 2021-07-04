using System;

namespace PipelineRD.Builders
{
    public class RequestErrorBuilder
    {
        private RequestError _requestError;

        public RequestErrorBuilder()
        {
            Reset();
        }

        public RequestErrorBuilder WithException(Exception exception)
        {
            _requestError.Exception = exception;
            return this;
        }

        public RequestErrorBuilder WithSource(string source)
        {
            _requestError.Source = source;
            return this;
        }

        public RequestErrorBuilder WithMessage(string message)
        {
            _requestError.Message = message;
            return this;
        }

        public RequestErrorBuilder WithResult(object result)
        {
            _requestError.Result = result;
            return this;
        }

        public RequestErrorBuilder WithProperty(string property)
        {
            _requestError.Property = property;
            return this;
        }

        public RequestErrorBuilder Reset()
        {
            _requestError = new RequestError();
            return this;
        }

        public RequestError Build() => _requestError;

        public static RequestErrorBuilder Instance()
            => new RequestErrorBuilder();
    }
}
