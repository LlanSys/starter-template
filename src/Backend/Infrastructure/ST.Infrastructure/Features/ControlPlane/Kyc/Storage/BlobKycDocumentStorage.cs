using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Features.ControlPlane.Kyc.Storage;

internal sealed class BlobKycDocumentStorage(IOptions<KycDocumentStorageSettings> configuration) : IKycDocumentStorage, IKycEvidenceLifecycleStorage
{
    private readonly Lazy<BlobContainerClient> _container = new(() => CreateClient(configuration.Value));

    private readonly Lazy<BlobContainerClient> _ledger = new(() => CreateLedger(configuration.Value));

    public async Task<bool> IsErasureRecordedAsync(string key, CancellationToken cancellationToken)
    {
        return (await _ledger.Value.GetBlobClient(key).ExistsAsync(cancellationToken).ConfigureAwait(false)).Value;
    }

    public async Task RecordErasureAsync(string key, CancellationToken cancellationToken)
    {
        await _ledger.Value.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
        var access = await _ledger.Value.GetAccessPolicyAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (access.Value.BlobPublicAccess != PublicAccessType.None) throw new IOException("The erasure ledger must be private.");
        // Only opaque storage identity is retained. An immutable create is idempotent across retries.
        try
        {
            await _ledger.Value.GetBlobClient(key).UploadAsync(BinaryData.FromString("erasure-committed-v1"), overwrite: false, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 409 && exception.ErrorCode == "BlobAlreadyExists")
        {
            // A previous attempt committed the same erasure intent.
        }
    }

    public async Task<bool> PurgeAsync(string key, CancellationToken cancellationToken)
    {
        var states = BlobStates.Snapshots | BlobStates.Deleted;

        if (configuration.Value.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase)) states |= BlobStates.Version | BlobStates.DeletedWithVersions;
        await _container.Value.GetBlobClient(key).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);

        await foreach (var item in _container.Value.GetBlobsAsync(traits: BlobTraits.None, states: states, prefix: key, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (item.Name != key || item.Deleted) continue;
            var blob = _container.Value.GetBlobClient(key);

            if (!string.IsNullOrEmpty(item.VersionId)) blob = blob.WithVersion(item.VersionId);
            else if (!string.IsNullOrEmpty(item.Snapshot)) blob = blob.WithSnapshot(item.Snapshot);
            var snapshots = string.IsNullOrEmpty(item.Snapshot) && string.IsNullOrEmpty(item.VersionId)
                ? DeleteSnapshotsOption.IncludeSnapshots : DeleteSnapshotsOption.None;

            await blob.DeleteIfExistsAsync(snapshots, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        await _container.Value.GetBlobClient(key).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);

        await foreach (var item in _container.Value.GetBlobsAsync(traits: BlobTraits.None, states: states, prefix: key, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (item.Name == key) return false; // Includes soft-deleted versions awaiting provider expiry.
        }
        return true;
    }

    public async Task<KycStoragePage> ListAsync(string? cursor, CancellationToken cancellationToken)
    {
        if (!(await _container.Value.ExistsAsync(cancellationToken).ConfigureAwait(false)).Value) return new KycStoragePage([], null);

        await foreach (var page in _container.Value.GetBlobsAsync(traits: BlobTraits.None, states: BlobStates.None,
            prefix: "kyc/", cancellationToken: cancellationToken).AsPages(cursor, pageSizeHint: 100).ConfigureAwait(false))
        {
            return new KycStoragePage(page.Values.Where(x => x.Properties.LastModified.HasValue)
                .Select(x => new KycStoredObject(x.Name, x.Properties.LastModified!.Value)).ToArray(), page.ContinuationToken);
        }
        return new KycStoragePage([], null);
    }

    private static BlobContainerClient CreateLedger(KycDocumentStorageSettings settings)
    {
        var originals = CreateClient(settings);
        return originals.GetParentBlobServiceClient().GetBlobContainerClient(settings.ErasureLedgerContainerName);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var container = _container.Value;
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
        var access = await container.GetAccessPolicyAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (access.Value.BlobPublicAccess != PublicAccessType.None)
        {
            throw new IOException("KYC document storage must be private.");
        }

        await container.GetBlobClient(key).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = "no-store" }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.Value.GetBlobClient(key)
                .DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return response.Value.Content;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await _container.Value.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static BlobContainerClient CreateClient(KycDocumentStorageSettings settings)
    {
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03);
        options.Retry.MaxRetries = 2;
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);

        var provider = Enum.Parse<KycDocumentStorageProvider>(settings.Provider, ignoreCase: true);
        return provider switch
        {
            KycDocumentStorageProvider.Azurite when !string.IsNullOrWhiteSpace(settings.ConnectionString) =>
                new BlobContainerClient(settings.ConnectionString, settings.ContainerName, options),
            KycDocumentStorageProvider.AzureBlob when !string.IsNullOrWhiteSpace(settings.ContainerUri) =>
                new BlobContainerClient(new Uri(settings.ContainerUri), new DefaultAzureCredential(), options),
            _ => throw new IOException("Configure Azurite ConnectionString or AzureBlob ContainerUri for KYC documents.")
        };
    }
}
