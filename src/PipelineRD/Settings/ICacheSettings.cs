namespace PipelineRD.Settings
{
    public interface ICacheSettings
    {
        int TTLInMinutes { get; set; }
        string KeyPreffix { get; set; }
    }
}
