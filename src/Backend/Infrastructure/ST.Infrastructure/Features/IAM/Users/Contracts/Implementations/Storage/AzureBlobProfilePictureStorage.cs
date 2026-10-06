using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Storage;

internal sealed class AzureBlobProfilePictureStorage(IOptions<ProfileImageStorageSettings> options) : IProfilePictureStorage
{
    private readonly ProfileImageStorageSettings _settings = options.Value;

    private AzureBlobProfileImageStorageSettings ActiveStorageSettings =>
    string.Equals(_settings.Provider, "Azurite", StringComparison.OrdinalIgnoreCase)
        ? _settings.Azurite
        : _settings.AzureBlob;

    public async Task<Uri> SaveAsync(
        string userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(content);

        var extension = GetSafeExtension(fileName, contentType);
        var safeUserId = string.Concat(userId.Select(static character =>
            char.IsLetterOrDigit(character) ? character : '-'));
        var storageSettings = ActiveStorageSettings;
        var blobPrefix = storageSettings.BlobPrefix.Trim().Trim('/');
        var storedFileName = $"{RandomNumberGenerator.GetHexString(12).ToLowerInvariant()}{extension}";
        var blobName = string.IsNullOrWhiteSpace(blobPrefix)
            ? $"{safeUserId}/{storedFileName}"
            : $"{blobPrefix}/{safeUserId}/{storedFileName}";
        var container = CreateContainerClient(storageSettings);

        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000, immutable"
                }
            },
            cancellationToken).ConfigureAwait(false);

        return blob.Uri;
    }

    public async Task<ProfilePictureFile?> OpenReadAsync(
        Uri profilePictureUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profilePictureUri);

        if (!profilePictureUri.IsAbsoluteUri)
        {
            // ToDo: Log warning about invalid profile picture URI
            return null;
        }

        var container = CreateContainerClient(ActiveStorageSettings);
        var blobName = GetBlobName(profilePictureUri, container.Uri);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            // ToDo: Log warning about profile picture URI not matching container URI
            return null;
        }

        var blob = container.GetBlobClient(blobName);
        try
        {
            var download = await blob
                .DownloadStreamingAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var contentType = string.IsNullOrWhiteSpace(download.Value.Details.ContentType)
                ? "application/octet-stream"
                : download.Value.Details.ContentType;
            var fileName = Path.GetFileName(blobName);

            return new ProfilePictureFile(download.Value.Content, contentType, fileName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && ex is RequestFailedException or HttpRequestException or AggregateException or IOException or TimeoutException or OperationCanceledException)
        {
            throw new HttpRequestException("Profile image storage is unavailable.", ex);
        }
    }

    private static BlobContainerClient CreateContainerClient(AzureBlobProfileImageStorageSettings settings)
    {
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03);
        options.Retry.MaxRetries = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);
        options.Retry.MaxDelay = TimeSpan.FromSeconds(1);
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);

        if (!string.IsNullOrWhiteSpace(settings.ContainerUri))
        {
            return new BlobContainerClient(new Uri(settings.ContainerUri), new DefaultAzureCredential(), options);
        }

        if (!string.IsNullOrWhiteSpace(settings.ConnectionString) &&
            !string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            return new BlobContainerClient(settings.ConnectionString, settings.ContainerName, options);
        }

        throw new InvalidOperationException(
            "Profile image blob storage requires either ContainerUri for managed identity or ConnectionString plus ContainerName.");
    }

    private static string? GetBlobName(Uri profilePictureUri, Uri containerUri)
    {
        var containerPrefix = containerUri.ToString().TrimEnd('/') + "/";
        var profilePictureUrl = profilePictureUri.ToString();
        if (!profilePictureUrl.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Uri.UnescapeDataString(profilePictureUrl[containerPrefix.Length..]);
    }

    private static string GetSafeExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ".jpg",
                ".png" => ".png",
                ".webp" => ".webp",
                _ => throw new InvalidOperationException("Unsupported profile image extension.")
            };
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Unsupported profile image content type.")
        };
    }
}
