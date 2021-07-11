using FluentValidation;

using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

using PipelineRD.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PipelineRD.Builders
{
    internal class PipelineRDBuilder : IPipelineRDBuilder
    {
        private readonly IServiceCollection _services;

        public bool CacheSettingsIsConfigured { get; private set; }
        private bool _cacheIsConfigured;
        private IEnumerable<TypeInfo> _types;

        public PipelineRDBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void UseDocumentation(Action<IDocumentationBuilder> configure)
        {
            if(_types == null)
            {
                _types = GetTypes();
            }

            var documentationBuilder = new DocumentationBuilder(_types, _services.BuildServiceProvider());
            configure(documentationBuilder);
        }

        private void UseCacheSettings(ICacheSettings settings)
        {
            _services.AddSingleton(settings);
            CacheSettingsIsConfigured = true;
        }

        public void UseCacheInMemory(MemoryCacheSettings cacheSettings)
        {
            if (_cacheIsConfigured) return;

            UseCacheSettings(cacheSettings);

            _services.AddDistributedMemoryCache();
            UseCacheProvider();
            _cacheIsConfigured = true;
        }

        public void UseCacheInRedis(RedisCacheSettings cacheSettings)
        {
            if (_cacheIsConfigured) return;

            UseCacheSettings(cacheSettings);

            var redisOptions = new RedisCacheOptions()
            {
                Configuration = cacheSettings.ConnectionString
            };

            _services.AddStackExchangeRedisCache(options =>
            {
                options = redisOptions;
            });

            UseCacheProvider();
            _cacheIsConfigured = true;
        }

        private void UseCacheProvider()
            => _services.AddSingleton<ICacheProvider, CacheProvider>();

        public void AddPipelineServices()
        {
            if (_types == null)
            {
                _types = GetTypes();
            }

            InjectContexts();
            InjectSteps();
            InjectRequestValidators();
            InjectPipelineBuilders();
            InjectPipelineInitializers();
            InjectPipelines();
        }

        private void InjectContexts()
        {
            var contexts = _types.Where(a => a.IsClass && a.BaseType == typeof(BaseContext));

            var duplicatedContexts = contexts.GroupBy(x => x.Name)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);

            if (duplicatedContexts.Any())
            {
                throw new Exception($"It cannot exists two pipeline contexts with the same name. Please, change the name of the context(s): {string.Join(", ", duplicatedContexts)}");
            }

            foreach (var context in contexts)
                _services.AddScoped(context.AsType());
        }

        private void InjectSteps()
        {
            var searchClasses = new Type[] { typeof(RequestStep<>), typeof(RollbackRequestStep<>) };

            var steps = _types
                .Where(x => !x.IsAbstract && x.IsClass && searchClasses.Any(t => IsSubclassOfGeneric(x, t)))
                .Select(x => new
                {
                    Type = x.AsType(),
                    Interface = GetInterfaces(x, false).FirstOrDefault()
                });

            foreach (var step in steps)
            {
                _services.AddScoped(step.Interface, step.Type);
            }
        }

        private void InjectPipelines()
            => _services.AddTransient(typeof(IPipeline<>), typeof(Pipeline<>));

        private void InjectRequestValidators()
        {
            var validators = from type in _types
                where !type.IsAbstract && !type.IsGenericTypeDefinition
                let interfaces = type.GetInterfaces()
                let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                let matchingInterface = genericInterfaces.FirstOrDefault()
                where matchingInterface != null
                select new { Interface = matchingInterface, Type = type };

            foreach (var validator in validators)
            {
                _services.AddSingleton(validator.Interface, validator.Type);
            }
        }

        private void InjectPipelineInitializers()
            => _services.AddSingleton(typeof(IPipelineInitializer<>), typeof(PipelineInitializer<>));

        private void InjectPipelineBuilders()
        {
            var pipelineBuilders = from type in _types
                where !type.IsAbstract && !type.IsGenericTypeDefinition
                let interfaces = type.GetInterfaces()
                let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBuilder<>))
                let matchingInterface = genericInterfaces.FirstOrDefault()
                where matchingInterface != null
                select new { Interface = matchingInterface, Type = type };

            foreach (var builder in pipelineBuilders)
                _services.AddTransient(builder.Interface, builder.Type);
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

        private static IEnumerable<TypeInfo> GetTypes()
            => GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(x => x != typeof(CompilerGeneratedAttribute))
            .Select(a => a.GetTypeInfo());
        #endregion
    }
}
