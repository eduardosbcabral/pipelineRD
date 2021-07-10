namespace PipelineRD.Settings
{
    public abstract class BaseCacheSettings : ICacheSettings
    {
        public int TTLInMinutes { get; set; } = 1;
        public string KeyPreffix { get; set; }
    }
}
