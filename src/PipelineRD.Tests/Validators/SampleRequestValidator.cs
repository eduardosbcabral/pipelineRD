using FluentValidation;

using PipelineRD.Tests.Request;

namespace PipelineRD.Tests.Validators
{
    public class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(x => x.ValidModel)
                .Equal(true);
        }
    }
}
