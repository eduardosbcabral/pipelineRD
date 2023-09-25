using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PipelineRD.Validation
{

    public static class PipelineExtensions
    {
        /// <summary>
        /// Execute the pipeline with a fail-fast validation for the request model using the fluent validation package.
        /// The second parameter is an optional validator that will be used if passed it. If not, it will try to get one from the DI container.
        /// </summary>
        /// <param name="request">A model that holds the data from the request.</param>
        /// <param name="validator">An optional validator that will be used if passed it. If not, it will try to get one from the DI container.</param>
        /// <returns>The result from the pipeline.</returns>
        public static async Task<HandlerResult> ExecuteWithValidation<TContext, TRequest>(
            this IPipeline<TContext, TRequest> pipeline,
            TRequest request,
            IValidator validator = null,
            HttpStatusCode defaultValidationFailStatus = HttpStatusCode.BadRequest)
            where TContext : BaseContext
        {
            // Make sure that we execute the validation when
            // it is not the PipelineDiagram
            if (pipeline.GetType() == typeof(Pipeline<TContext, TRequest>))
            {
                if (validator == null)
                {
                    var injectedValidator = pipeline.ServiceProvider.GetService<IValidator<TRequest>>();
                    validator = injectedValidator ?? throw new PipelineException($"There is no validator injected in DI for this request type({request.GetType().Name}). Please pass a validator to the method 'ExecuteWithValidation' or inject it.");
                }

                if (validator != null)
                {
                    var validationContext = new ValidationContext<TRequest>(request);
                    var validateResult = validator.Validate(validationContext);

                    if (!validateResult.IsValid)
                    {
                        var errors = validateResult.Errors
                            .Select(p => new HandlerError()
                            {
                                Message = p.ErrorMessage,
                                Source = p.PropertyName
                            })
                            .ToList();

                        return HandlerResultBuilder.CreateDefault()
                                .WithErrors(errors)
                                .WithStatusCode(defaultValidationFailStatus);
                    }
                }
            }

            return await pipeline.Execute(request);
        }
    }
}
