using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Storage;

internal sealed class S3ProfilePictureStorage(IOptions<ProfileImageStorageSettings> options) : IProfilePictureStorage
{
    private readonly ProfileImageStorageSettings _settings = options.Value;

    private S3ProfileImageStorageSettings ActiveStorageSettings =>
        _settings.S3;

    public async Task<Uri> SaveAsync(
        string userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var activeSettings = ActiveStorageSettings;
        using var s3Client = CreateS3Client(activeSettings);

        var blobName = $"{activeSettings.ObjectPrefix.TrimEnd('/')}/{userId}/{fileName}";

        var putRequest = new PutObjectRequest
        {
            BucketName = activeSettings.BucketName,
            Key = blobName,
            InputStream = content,
            ContentType = contentType
        };

        await s3Client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);

        // Pre-signed URL or direct URL. Assuming public read for profile images, we can construct the direct URL.
        // Wait, S3 usually returns object URLs like: https://<bucket>.s3.<region>.amazonaws.com/<key>
        // If ServiceUrl is provided, it's https://<bucket>.<serviceUrl>/<key> or https://<serviceUrl>/<bucket>/<key>
        // It's safer to generate a pre-signed URL or build it. Wait, the template returns the raw URI which the frontend might need.
        // Let's use the standard format.

        var uriBuilder = new UriBuilder(activeSettings.ServiceUrl ?? "https://s3.amazonaws.com");
        uriBuilder.Path = $"{activeSettings.BucketName}/{blobName}";

        return uriBuilder.Uri;
    }

    public async Task<ProfilePictureFile?> OpenReadAsync(
        Uri profilePictureUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profilePictureUri);
        var activeSettings = ActiveStorageSettings;
        using var s3Client = CreateS3Client(activeSettings);

        // Parse the blobName from the URI
        var path = profilePictureUri.AbsolutePath.TrimStart('/');
        var bucketPrefix = $"{activeSettings.BucketName}/";
        var blobName = path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase)
            ? path[bucketPrefix.Length..]
            : path;

        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = activeSettings.BucketName,
                Key = blobName
            };

            var response = await s3Client.GetObjectAsync(getRequest, cancellationToken).ConfigureAwait(false);
            return new ProfilePictureFile(response.ResponseStream, response.Headers.ContentType, Path.GetFileName(blobName));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    internal static AmazonS3Client CreateS3Client(S3ProfileImageStorageSettings settings)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = settings.ServiceUrl,
            ForcePathStyle = true // Use path style to support MinIO and various other S3 compatibles properly
        };

        return new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
    }
}
