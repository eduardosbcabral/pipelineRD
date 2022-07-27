using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace PipelineRD.Extensions.Builders;

public class PipelineServicesBuilder : IPipelineServicesBuilder
{
    public IEnumerable<TypeInfo> Types { get; private set; }
    public IServiceCollection Services { get; private set; }

    private bool _contextsAlreadySet;
    private bool _handlersAlreadySet;
    private bool _pipelinesAlreadySet;

    public PipelineServicesBuilder(IEnumerable<TypeInfo> types, IServiceCollection services)
    {
        Types = types;
        Services = services;
    }

    public void InjectAll(ServiceLifetime servicesLifetime = ServiceLifetime.Scoped)
    {
        InjectContexts(servicesLifetime);
        InjectHandlers(servicesLifetime);
        InjectPipelines(servicesLifetime);
    }

    public void InjectContexts(ServiceLifetime contextsLifetime = ServiceLifetime.Scoped)
    {
        if (_contextsAlreadySet) return;

        var contexts = Types.Where(a => a.IsClass && a.BaseType == typeof(BaseContext));

        foreach (var context in contexts)
        {
            _ = contextsLifetime switch
            {
                ServiceLifetime.Scoped => Services.AddScoped(context.AsType()),
                ServiceLifetime.Transient => Services.AddTransient(context.AsType()),
                ServiceLifetime.Singleton => Services.AddSingleton(context.AsType()),
                _ => null
            };
        }

        _contextsAlreadySet = true;
    }

    public void InjectPipelines(ServiceLifetime pipelinesLifetime = ServiceLifetime.Scoped)
    {
        if (_pipelinesAlreadySet) return;

        _ = pipelinesLifetime switch
        {
            ServiceLifetime.Scoped => Services.AddScoped(typeof(IPipeline<,>), typeof(Pipeline<,>)),
            ServiceLifetime.Transient => Services.AddTransient(typeof(IPipeline<,>), typeof(Pipeline<,>)),
            ServiceLifetime.Singleton => Services.AddScoped(typeof(IPipeline<,>), typeof(Pipeline<,>)),
            _ => null
        };

        _pipelinesAlreadySet = true;
    }

    public void InjectHandlers(ServiceLifetime handlersLifetime = ServiceLifetime.Scoped)
    {
        if (_handlersAlreadySet) return;

        var searchClasses = new Type[] { typeof(Handler<,>), typeof(RecoveryHandler<,>) };

        var handlers = Types.Where(x => !x.IsAbstract && x.IsClass && searchClasses.Any(t => IsSubclassOfGeneric(x, t)));

        foreach (var handler in handlers)
        {
            _ = handlersLifetime switch
            {
                ServiceLifetime.Scoped => Services.AddScoped(handler.AsType()),
                ServiceLifetime.Transient => Services.AddTransient(handler.AsType()),
                ServiceLifetime.Singleton => Services.AddSingleton(handler.AsType()),
                _ => null
            };
        }

        _handlersAlreadySet = true;
    }

    #region Generic helpers methods 
    private static bool IsSubclassOfGeneric(Type current, Type genericBase)
    {
        do
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBase)
                return true;
        }
        while ((current = current.BaseType) != null);
        return false;
    }

    public static IEnumerable<Type> GetInterfaces(Type type, bool includeInherited)
    {
        if (includeInherited || type.BaseType == null)
            return type.GetInterfaces();
        else
            return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
    }
    #endregion
}

public interface IPipelineServicesBuilder
{
    void InjectContexts(ServiceLifetime contextsLifetime = ServiceLifetime.Scoped);
    void InjectHandlers(ServiceLifetime handlersLifetime = ServiceLifetime.Scoped);
    void InjectPipelines(ServiceLifetime pipelinesLifetime = ServiceLifetime.Scoped);
    void InjectAll(ServiceLifetime servicesLifetime = ServiceLifetime.Scoped);
}