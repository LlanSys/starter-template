namespace ST.Infrastructure.Configuration;

public sealed class S3ProfileImageStorageSettings
{
    public string? ServiceUrl { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? BucketName { get; init; }
    public string ObjectPrefix { get; init; } = "profile-images";
}
