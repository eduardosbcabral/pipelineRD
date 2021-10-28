using System.Collections.Generic;
using System.Net;

using WebApi.Models.Response;

namespace PipelineRD
{
    public class RequestStepHandlerResultBuilder
    {
        private RequestStepResult Handler;

        public RequestStepHandlerResultBuilder()
        {
            this.Reset();
        }

        public RequestStepHandlerResultBuilder WithErrors(List<RequestError> errors)
        {
            if (errors == null)
                return this;

            foreach (var error in errors)
            {
                if (error != null)
                {
                    this.Handler.SetError(error);
                }
            }
            return this;
        }

        public RequestStepHandlerResultBuilder WithError(RequestError error)
        {
            if (error == null) 
                return this;

            this.Handler.SetError(error);
            return this;
        }

        public RequestStepHandlerResultBuilder WithResultErrorItems(params ErrorItemResponse[] errors)
        {
            this.Handler.SetResultErrorItems(errors);
            return this;
        }

        public RequestStepHandlerResultBuilder WithStatusCode(int statusCode)
        {
            this.Handler.StatusCode = statusCode;
            return this;
        }

        public RequestStepHandlerResultBuilder WithHttpStatusCode(HttpStatusCode statusCode)
        {
            this.Handler.StatusCode = (int)statusCode;
            return this;
        }

        public RequestStepHandlerResultBuilder WithSuccess()
        {
            this.Handler.SetSucess();
            return this;
        }

        public RequestStepHandlerResultBuilder WithFailure()
        {
            this.Handler.SetFailure();
            return this;
        }

        public RequestStepHandlerResultBuilder WithRequestStepIdentifier(string identifier)
        {
            this.Handler.SetRequestStepIdentifier(identifier);
            return this;
        }

        public RequestStepHandlerResultBuilder WithResultObject(object resultObject)
        {
            this.Handler.SetResultObject(resultObject);
            return this;
        }

        public RequestStepHandlerResultBuilder WithErrorMessage(string errorMessage)
        {
            this.Handler.SetErrorMessage(errorMessage);
            return this;
        }

        public void Reset() => this.Handler = new RequestStepResult();

        public RequestStepResult Build() => this.Handler;

        public static RequestStepHandlerResultBuilder Instance() => new RequestStepHandlerResultBuilder();
    }
}
