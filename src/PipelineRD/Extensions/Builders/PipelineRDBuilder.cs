using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

using System.Reflection;
using System.Runtime.CompilerServices;

using PipelineRD.Cache;

namespace PipelineRD.Extensions.Builders;

public class PipelineRDBuilder : IPipelineRDBuilder
{
    private readonly IServiceCollection _services;

    private IEnumerable<TypeInfo> _types;

    public PipelineRDBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public void SetupCache(IPipelineRDCacheSettings cacheSettings)
    {
        _services.AddSingleton(cacheSettings);
        _services.AddSingleton<ICacheProvider, CacheProvider>();
    }

    public void SetupPipelineServices(Action<IPipelineServicesBuilder> configure)
    {
        _types ??= GetTypes();

        var builder = new PipelineServicesBuilder(_types, _services);
        configure(builder);
    }

    #region Helpers methods 
    private static IEnumerable<Assembly> GetAssemblies()
    {
        var dependencies = DependencyContext.Default.RuntimeLibraries.Where(p =>
            p.Type.Equals("Project", StringComparison.CurrentCultureIgnoreCase));

        foreach (var library in dependencies)
        {
            var name = new AssemblyName(library.Name);
            var assembly = Assembly.Load(name);
            yield return assembly;
        }
    }

    private IEnumerable<TypeInfo> GetTypes()
        => _types?.Any() == true 
            ? _types 
            : GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(x => x != typeof(CompilerGeneratedAttribute))
                .Select(a => a.GetTypeInfo());
    #endregion

}

public interface IPipelineRDBuilder
{
    void SetupCache(IPipelineRDCacheSettings cacheSettings);
    void SetupPipelineServices(Action<IPipelineServicesBuilder> configure);
}