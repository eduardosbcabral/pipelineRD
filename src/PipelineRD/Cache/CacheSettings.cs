namespace PipelineRD.Cache;

public class PipelineRDCacheSettings : IPipelineRDCacheSettings
{
    public int TTLInMinutes { get; set; } = 1;
    public string KeyPreffix { get; set; }
}

public interface IPipelineRDCacheSettings
{
    int TTLInMinutes { get; set; }
    string KeyPreffix { get; set; }
}
