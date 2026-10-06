namespace ST.Infrastructure.Configuration;

public sealed class AzureCacheSettings
{
    public string? ConnectionString { get; set; }
    public bool UseEntraId { get; set; }
    public string? PrincipalId { get; set; }
}
