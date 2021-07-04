using System.Collections.Generic;
using System.Linq;

using WebApi.Models.Response;

namespace PipelineRD
{
    public class RequestStepResult
    {
        public bool Success { get; private set; }

        public object ResultObject { get; private set; }

        public string RequestStepIdentifier { get; set; }

        public IList<RequestError> Errors { get; private set; }

        public int StatusCode { get; set; }

        public RequestStepResult()
        { }

        public bool IsSuccess() => this.Success;

        public object Result() => this.ResultObject;

        internal void SetStatusCode(int statusCode)
            => this.StatusCode = statusCode;

        internal void SetErrors(params RequestError[] errors)
            => this.Errors = errors;

        internal void SetResultErrorItems(params ErrorItemResponse[] errors)
            => this.SetResultObject(new ErrorsResponse
            {
                Errors = errors.ToList()
            });

        internal void SetSucess()
            => this.Success = true;

        internal void SetFailure()
            => this.Success = false;

        internal void SetResultObject(object resultObject)
            => this.ResultObject = resultObject;

        internal void SetErrorMessage(string errorMessage)
            => this.SetErrors(new RequestError(errorMessage));

        internal void SetRequestStepIdentifier(string identifier)
            => this.RequestStepIdentifier = identifier;
    }
}
