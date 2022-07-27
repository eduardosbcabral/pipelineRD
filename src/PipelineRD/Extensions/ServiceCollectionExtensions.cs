using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Extensions.Builders;

namespace PipelineRD.Extensions;

public static class ServiceCollectionExtensions
{
    public static void UsePipelineRD(this IServiceCollection services, Action<IPipelineRDBuilder> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var builder = new PipelineRDBuilder(services);
        configure(builder);
    }
}