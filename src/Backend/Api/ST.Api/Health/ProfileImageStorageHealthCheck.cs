using Amazon.S3;
using Azure.Identity;
using Azure.Storage.Blobs;

using ST.Infrastructure.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ST.Api.Health;

internal sealed class ProfileImageStorageHealthCheck(
    IWebHostEnvironment environment,
    IOptions<ProfileImageStorageSettings> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var provider = settings.Provider?.Trim() ?? "Local";

        if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            return CheckLocalStorage(settings);
        }

        if (provider.Equals("Azurite", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckBlobStorageAsync(settings.Azurite, "Azurite profile image storage", cancellationToken)
                .ConfigureAwait(false);
        }

        if (provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckBlobStorageAsync(settings.AzureBlob, "Azure Blob profile image storage", cancellationToken)
                .ConfigureAwait(false);
        }
        if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckS3StorageAsync(settings.S3, "S3 profile image storage", cancellationToken)
                .ConfigureAwait(false);
        }

        return HealthCheckResult.Unhealthy($"ProfileImageStorage:Provider '{settings.Provider}' is not supported.");
    }

    private HealthCheckResult CheckLocalStorage(ProfileImageStorageSettings settings)
    {
        try
        {
            var rootPath = Path.IsPathRooted(settings.LocalRootPath)
                ? settings.LocalRootPath
                : Path.Combine(environment.ContentRootPath, settings.LocalRootPath);

            Directory.CreateDirectory(rootPath);
            return HealthCheckResult.Healthy("Local profile image storage is writable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Local profile image storage probe failed.", ex);
        }
    }

    private static async Task<HealthCheckResult> CheckBlobStorageAsync(
        AzureBlobProfileImageStorageSettings settings,
        string description,
        CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = CreateContainerClient(settings);
            var exists = await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

            return exists.Value
                ? HealthCheckResult.Healthy($"{description} container is reachable.")
                : HealthCheckResult.Unhealthy($"{description} container does not exist or cannot be accessed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{description} probe failed.", ex);
        }
    }

    private static BlobContainerClient CreateContainerClient(AzureBlobProfileImageStorageSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ContainerUri))
        {
            return new BlobContainerClient(new Uri(settings.ContainerUri), new DefaultAzureCredential());
        }

        if (!string.IsNullOrWhiteSpace(settings.ConnectionString) &&
            !string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            return new BlobContainerClient(settings.ConnectionString, settings.ContainerName);
        }

        throw new InvalidOperationException("Blob profile image storage requires ContainerUri or ConnectionString plus ContainerName.");
    }

    private static async Task<HealthCheckResult> CheckS3StorageAsync(
        S3ProfileImageStorageSettings settings,
        string description,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = new AmazonS3Config
            {
                ServiceURL = settings.ServiceUrl,
                ForcePathStyle = true
            };

            using var client = new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);

            var request = new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = settings.BucketName,
                MaxKeys = 1
            };

            await client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy($"{description} bucket is reachable.");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return HealthCheckResult.Unhealthy($"{description} bucket does not exist.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{description} probe failed.", ex);
        }
    }
}
