using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Storage;

internal sealed class LocalProfilePictureStorage(
    IWebHostEnvironment environment,
    IOptions<ProfileImageStorageSettings> options) : IProfilePictureStorage
{
    private readonly ProfileImageStorageSettings _settings = options.Value;

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
        var storedFileName = $"{safeUserId}-{RandomNumberGenerator.GetHexString(12).ToLowerInvariant()}{extension}";
        var relativeRoot = _settings.LocalRootPath
            .Trim()
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var webRootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var targetDirectory = Path.Combine(webRootPath, relativeRoot);

        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, storedFileName);
        using var fileStream = File.Create(targetPath);
        await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

        var publicPath = $"{_settings.PublicBasePath.TrimEnd('/')}/{storedFileName}";
        return new Uri(publicPath, UriKind.Relative);
    }

    public Task<ProfilePictureFile?> OpenReadAsync(
        Uri profilePictureUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profilePictureUri);

        if (profilePictureUri.IsAbsoluteUri)
        {
            return Task.FromResult<ProfilePictureFile?>(null);
        }

        var publicBasePath = _settings.PublicBasePath.TrimEnd('/');
        var relativeUri = profilePictureUri.ToString();
        if (!relativeUri.StartsWith(publicBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<ProfilePictureFile?>(null);
        }

        var fileName = Path.GetFileName(relativeUri);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult<ProfilePictureFile?>(null);
        }

        var relativeRoot = _settings.LocalRootPath
            .Trim()
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var webRootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var targetDirectory = Path.GetFullPath(Path.Combine(webRootPath, relativeRoot));
        var targetPath = Path.GetFullPath(Path.Combine(targetDirectory, fileName));

        if (!targetPath.StartsWith(targetDirectory, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(targetPath))
        {
            return Task.FromResult<ProfilePictureFile?>(null);
        }

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        if (!contentTypeProvider.TryGetContentType(targetPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var stream = File.OpenRead(targetPath);
        return Task.FromResult<ProfilePictureFile?>(new ProfilePictureFile(stream, contentType, fileName));
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
