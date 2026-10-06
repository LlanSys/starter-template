namespace ST.Infrastructure.Configuration;

public sealed class AzureBlobProfileImageStorageSettings
{
    public string? ContainerUri { get; init; }
    public string? ConnectionString { get; init; }
    public string? ContainerName { get; init; }
    public string BlobPrefix { get; init; } = "profile-images";
}
