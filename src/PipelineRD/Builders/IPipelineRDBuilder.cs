using PipelineRD.Settings;

using System;

namespace PipelineRD.Builders
{
    public interface IPipelineRDBuilder
    {
        void UseCacheInMemory(MemoryCacheSettings cacheSettings);
        void UseCacheInRedis(RedisCacheSettings cacheSettings);
        void AddPipelineServices(Action<IPipelineServicesBuilder> configure);
        void UseDocumentation(string applicationName, Action<IDocumentationBuilder> configure);
    }
}
