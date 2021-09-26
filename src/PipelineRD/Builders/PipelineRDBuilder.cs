using Microsoft.Extensions.Caching.Distributed;
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
        private IDistributedCache _currentDistributedCache;

        public PipelineRDBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void UseDocumentation(string applicationName, Action<IDocumentationBuilder> configure)
        {
            if(_types == null)
            {
                _types = GetTypes();
            }

            var documentationBuilder = new DocumentationBuilder(applicationName, _types, _services.BuildServiceProvider());
            configure(documentationBuilder);
        }

        private void UseCacheSettings(ICacheSettings settings)
        {
            _services.AddSingleton(settings);

            var existentDistributedCache = _services.BuildServiceProvider().GetService<IDistributedCache>();
            if (existentDistributedCache != null)
            {
                _currentDistributedCache = existentDistributedCache;
            }

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

        /// <summary>
        /// Use cache in redis. If the redis is already being used, it will ignore the connection string passed as parameter, 
        /// but the additional settings (TTLInMinutes/KeyPreffix) are still needed and will be used.
        /// </summary>
        /// <param name="cacheSettings">The PipelineRD redis cache settings model.</param>
        public void UseCacheInRedis(RedisCacheSettings cacheSettings)
        {
            if (_cacheIsConfigured) return;

            UseCacheSettings(cacheSettings);

            if (_currentDistributedCache?.GetType() != typeof(RedisCache))
            {
                _services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cacheSettings.ConnectionString;
                });
            }

            UseCacheProvider();
            _cacheIsConfigured = true;
        }

        private void UseCacheProvider()
            => _services.AddSingleton<ICacheProvider, CacheProvider>();

        public void AddPipelineServices(Action<IPipelineServicesBuilder> configure)
        {
            if (_types == null)
            {
                _types = GetTypes();
            }

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

        private static IEnumerable<TypeInfo> GetTypes()
            => GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(x => x != typeof(CompilerGeneratedAttribute))
            .Select(a => a.GetTypeInfo());
        #endregion
    }
}
