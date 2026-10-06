namespace ST.Infrastructure.Configuration;

public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public string Provider { get; set; } = "Auto";
    public RedisCacheSettings Redis { get; set; } = new();
    public AzureCacheSettings? Azure { get; set; }
}
