using PipelineRD.Settings;

using System;

namespace PipelineRD.Builders
{
    public interface IPipelineRDBuilder
    {
        void UseCacheInMemory(MemoryCacheSettings cacheSettings);
        void UseCacheInRedis(RedisCacheSettings cacheSettings);
        void AddPipelineServices();
        void UseDocumentation(Action<IDocumentationBuilder> configure);
    }
}
