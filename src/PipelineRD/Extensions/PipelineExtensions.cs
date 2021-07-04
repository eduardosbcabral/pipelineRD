using FluentValidation;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using static FluentValidation.AssemblyScanner;

namespace PipelineRD.Extensions
{
    public static class PipelineExtensions
    {
        public static void SetupPipelineR(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var assemblies = GetAssemblies();

            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(x => x != typeof(CompilerGeneratedAttribute))
                .Select(a => a.GetTypeInfo());

            InjectContexts(services, types);
            InjectSteps(services, types);
            InjectPipelineBuilders(services, types);
            InjectPipelines(services);
            InjectPipelineInitializers(services);
            InjectPipelineRequestValidators(services, types);

            //Generate Docs
            //services.AddSingleton(p => new DocumentationBuilder());
            //ExecutePipelineStarting(services, types, typeof(PipelineStartingDiagram<>));
            //ServiceProvider = services.BuildServiceProvider();

            //LoadingDiagrams(types);

            //var documentation = ServiceProvider.GetService<DocumentationBuilder>();
            //documentation.Compile();

            //ExecutePipelineStarting(services, types, typeof(PipelineStarting<>));
        }

        public static void SetupPipelineRCacheInMemory(this IServiceCollection services, CacheSettings cacheSettings)
        {
            services.AddSingleton(cacheSettings);

            services.AddDistributedMemoryCache();
            var provider = services.BuildServiceProvider();
            var distributedCache = provider.GetService<IDistributedCache>();
            services.AddSingleton<ICacheProvider>(new CacheProvider(cacheSettings, distributedCache));
        }

        public static void SetupPipelineRCacheInRedis(this IServiceCollection services, CacheSettings cacheSettings, RedisCacheOptions redisCacheOptions)
        {
            services.AddSingleton(cacheSettings);
            services.AddStackExchangeRedisCache(options =>
            {
                options = redisCacheOptions;
            });
            var provider = services.BuildServiceProvider();
            var distributedCache = provider.GetService<IDistributedCache>();
            services.AddSingleton<ICacheProvider>(new CacheProvider(cacheSettings, distributedCache));
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

        private static void InjectContexts(IServiceCollection services, IEnumerable<TypeInfo> typeInfos)
        {
            var contexts = typeInfos.Where(a => a.IsClass && a.BaseType == typeof(BaseContext));

            var duplicatedContexts = contexts.GroupBy(x => x.Name)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);

            if(duplicatedContexts.Any())
            {
                throw new Exception($"It cannot exists two pipeline contexts with the same name. Please, change the name of the context(s): {string.Join(", ", duplicatedContexts)}");
            }

            foreach (var context in contexts)
                services.AddScoped(context.AsType());
        }

        private static void InjectSteps(IServiceCollection services, IEnumerable<TypeInfo> types)
        {
            var searchingCondition = new[] { "RequestStep", "RollbackRequestStep", "ICondition", "IPipelineBuilder" };

            var pipes = types
                .Where(a => a.IsClass &&
                    !searchingCondition.Any(exclude => a.Name.Contains(exclude)) &&
                    a.ImplementedInterfaces.Any(i =>
                        searchingCondition.Any(include => i.Name.Contains(include))));

            foreach (var pipe in pipes)
            {
                var interfaces = pipe.GetInterfaces()
                    .Where(a => !searchingCondition.Any(e => a.Name.Contains(e)));

                foreach (var i in interfaces)
                    services.AddScoped(i, pipe.AsType());
            }
        }

        private static void InjectPipelineBuilders(IServiceCollection services, IEnumerable<TypeInfo> types)
        {
            var interfaces = types.Where(a => a.IsInterface && a.GetInterfaces().Any(x => x.Name.Contains("IPipelineBuilder")));
            foreach (var builderInterface in interfaces)
            {
                var builder = types.Where(a => a.IsClass && a.GetInterfaces().Contains(builderInterface)).FirstOrDefault();
                services.AddScoped(builderInterface, builder);
            }
        }

        private static void InjectPipelines(IServiceCollection services)
        {
            services.AddTransient(typeof(IPipeline<>), typeof(Pipeline<>));
        }

        private static void InjectPipelineInitializers(IServiceCollection services)
        {
            services.AddScoped(typeof(IPipelineInitializer<>), typeof(PipelineInitializer<>));
        }

        private static void InjectPipelineRequestValidators(IServiceCollection services, IEnumerable<TypeInfo> types)
        {
            var validators = from type in types
                where !type.IsAbstract && !type.IsGenericTypeDefinition
                let interfaces = type.GetInterfaces()
                let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                let matchingInterface = genericInterfaces.FirstOrDefault()
                where matchingInterface != null
                select new AssemblyScanResult(matchingInterface, type);

            foreach (var validator in validators)
            {
                services.AddSingleton(validator.InterfaceType, validator.ValidatorType);
            }
        }
    }
}
