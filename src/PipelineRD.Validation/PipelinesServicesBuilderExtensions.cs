using FluentValidation;

using Microsoft.Extensions.DependencyInjection;
using PipelineRD.Extensions.Builders;
using System.Linq;

namespace PipelineRD.Validation
{
    public static class PipelinesServicesBuilderExtensions
    {
        public static void InjectRequestValidators(this IPipelineServicesBuilder builder)
        {
            var validators = from type in builder.Types
                             where !type.IsAbstract && !type.IsGenericTypeDefinition
                             let interfaces = type.GetInterfaces()
                             let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                             let matchingInterface = genericInterfaces.FirstOrDefault()
                             where matchingInterface != null
                             select new { Interface = matchingInterface, Type = type };

            foreach (var validator in validators)
            {
                builder.Services.AddSingleton(validator.Interface, validator.Type);
            }
        }
    }
}
