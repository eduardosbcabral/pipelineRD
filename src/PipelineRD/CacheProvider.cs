using JsonNet.ContractResolvers;

using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Polly;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PipelineRD
{
    public class CacheProvider : ICacheProvider
    {
        private readonly CacheSettings _cacheSettings;
        private readonly IDistributedCache _distributedCache;
        private JsonSerializer _serializer;

        public CacheProvider(CacheSettings cacheSettings, IDistributedCache distributedCache)
        {
            _cacheSettings = cacheSettings;
            _distributedCache = distributedCache;
        }
                
        private JsonSerializer GetSerializer()
        {
            if (_serializer == null)
                _serializer = new JsonSerializer()
                {
                    ContractResolver = new PrivateSetterContractResolver(),
                    Converters = { new ContextConverter() },
                    TypeNameHandling = TypeNameHandling.Auto
                };

            return _serializer;
        }

        public async Task<bool> Add<T>(T obj, string key)
        {
            key = GenerateKeyWithPreffix(key);

            await _distributedCache.RemoveAsync(key);

            var ttl = TimeSpan.FromMinutes(_cacheSettings.TTLInMinutes);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                GetSerializer().Serialize(writer, obj);

                var json = sb.ToString();

                await _distributedCache.SetStringAsync(key, json, options);
            }

            return true;
        }

        public async Task<T> Get<T>(string key)
        {
            key = GenerateKeyWithPreffix(key);

            var result = await Policy
                .Handle<Exception>()
                .RetryAsync(3)
                .ExecuteAsync(async () => await _distributedCache.GetStringAsync(key));

            if (string.IsNullOrWhiteSpace(result))
            {
                return default;
            }

            var jsonReader = new JsonTextReader(new StringReader(result));

            return GetSerializer().Deserialize<T>(jsonReader);
        }

        private string GenerateKeyWithPreffix(string key)
        {
            var keyWithPreffix = key;

            if(!string.IsNullOrWhiteSpace(_cacheSettings.KeyPreffix))
            {
                keyWithPreffix = $"{_cacheSettings.KeyPreffix}:pipeline:{key}";
            }

            return keyWithPreffix;
        }
    }

    public interface ICacheProvider
    {
        Task<T> Get<T>(string key);

        Task<bool> Add<T>(T obj, string key);
    }
}
