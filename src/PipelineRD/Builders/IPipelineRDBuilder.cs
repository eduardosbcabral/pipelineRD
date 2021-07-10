using PipelineRD.Settings;

namespace PipelineRD.Builders
{
    public interface IPipelineRDBuilder
    {
        void UseCacheInMemory(MemoryCacheSettings cacheSettings);
        void UseCacheInRedis(RedisCacheSettings cacheSettings);
        void AddPipelineServices();
    }
}
