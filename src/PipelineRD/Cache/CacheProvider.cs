using Microsoft.Extensions.Caching.Distributed;

using Polly;

using System.Text.Json;

namespace PipelineRD.Cache;

public class CacheProvider : ICacheProvider
{
    private readonly IPipelineRDCacheSettings _cacheSettings;
    private readonly IDistributedCache _distributedCache;
    private readonly JsonSerializerOptions _serializerOptions;

    public CacheProvider(IPipelineRDCacheSettings cacheSettings, IDistributedCache distributedCache)
    {
        _cacheSettings = cacheSettings ?? throw new PipelineException("Interface IPipelineRDCacheSettings is not configured.");
        _distributedCache = distributedCache ?? throw new PipelineException("Interface IDistributedCache is not configured.");
        _serializerOptions = new JsonSerializerOptions();
    }

    public async Task<bool> AddAsync<T>(T obj, string key)
    {
        key = GenerateKeyWithPreffix(key);

        await _distributedCache.RemoveAsync(key);

        var json = string.Empty;
        using (var stream = new MemoryStream())
        {
            await JsonSerializer.SerializeAsync(stream, obj, obj.GetType(), _serializerOptions);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            json = await reader.ReadToEndAsync();
        }

        await _distributedCache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheSettings.TTLInMinutes)
        });

        return true;
    }

    public bool Add<T>(T obj, string key)
    {
        key = GenerateKeyWithPreffix(key);

        _distributedCache.Remove(key);

        var json = JsonSerializer.Serialize(obj, obj.GetType(), _serializerOptions);

        _distributedCache.SetString(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheSettings.TTLInMinutes)
        });

        return true;
    }

    public async Task<T> GetAsync<T>(string key)
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

        return JsonSerializer.Deserialize<T>(result);
    }

    public T Get<T>(string key)
    {
        key = GenerateKeyWithPreffix(key);

        var result = Policy
            .Handle<Exception>()
            .Retry(3)
            .Execute(() => _distributedCache.GetString(key));

        if (string.IsNullOrWhiteSpace(result))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(result);
    }

    private string GenerateKeyWithPreffix(string key)
    {
        var keyWithPreffix = key;

        if (!string.IsNullOrWhiteSpace(_cacheSettings.KeyPreffix))
        {
            keyWithPreffix = $"{_cacheSettings.KeyPreffix}:pipeline:{key}";
        }

        return keyWithPreffix;
    }
}

public interface ICacheProvider
{
    Task<T> GetAsync<T>(string key);
    T Get<T>(string key);

    Task<bool> AddAsync<T>(T obj, string key);
    bool Add<T>(T obj, string key);
}