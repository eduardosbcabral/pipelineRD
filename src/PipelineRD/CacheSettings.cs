namespace PipelineRD
{
    public class CacheSettings
    {
        public int TTLInMinutes { get; set; } = 1;
        public string ConnectionString { get; set; }
        public string KeyPreffix { get; set; }
    }
}
