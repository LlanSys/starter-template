using Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Kyc.Enums;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class KycDocumentSafetyProcessor(IServiceScopeFactory scopes,
    IOptions<KycDocumentSafetySettings> options, IOptions<KycDocumentStorageSettings> storageOptions,
    IOptions<OrgSettings> tenantOptions,
    TimeProvider clock, ILogger<KycDocumentSafetyProcessor> logger) : IKycDocumentSafetyProcessor
{
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        Guid[] ids;

        await using (var scope = scopes.CreateAsyncScope())
        {
            var unit = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();
            var stamp = tenantOptions.Value.DeploymentStampId;

            if (stamp.HasValue && !await unit.Tenants.AnyAsync(tenant => tenant.Id == Tenant.PlatformTenantId
                && tenant.DeploymentStampId == stamp.Value, cancellationToken).ConfigureAwait(false))
            {
                // Evidence belongs to the control plane. Dedicated customer runtimes must not
                // lease the central queue using their own storage/scanner configuration.
                return 0;
            }

            ids = await unit.KycDocuments.FindAll().Where(item => item.ErasedAt == null &&
                (((item.SafetyStatus == KycDocumentSafetyStatus.Pending || item.SafetyStatus == KycDocumentSafetyStatus.Failed)
                    && item.NextSafetyScanAt != null && item.NextSafetyScanAt <= now)
                 || (item.SafetyStatus == KycDocumentSafetyStatus.Scanning && (item.SafetyLeaseUntil == null || item.SafetyLeaseUntil <= now))))
                .OrderBy(item => item.UploadedAt).ThenBy(item => item.Id).Take(settings.BatchSize)
                .Select(item => item.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        var completed = 0;

        foreach (var id in ids)
        {
            await using var scope = scopes.CreateAsyncScope();
            var unit = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();

            try
            {
                var document = await unit.KycDocuments.FindAll().AsTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);

                if (document is null)
                {
                    continue;
                }

                var leaseId = Guid.CreateVersion7();
                var began = document.TryBeginSafetyScan(leaseId, clock.GetUtcNow(), TimeSpan.FromSeconds(settings.LeaseSeconds), settings.MaxAttempts);
                await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);

                if (!began)
                {
                    continue;
                }

                var storage = scope.ServiceProvider.GetRequiredService<IKycDocumentStorage>();
                var ledger = scope.ServiceProvider.GetRequiredService<IKycEvidenceLifecycleStorage>();
                KycScanResult result;

                try
                {
                    if (await ledger.IsErasureRecordedAsync($"cases/{document.TenantId:N}", cancellationToken).ConfigureAwait(false)
                        || await ledger.IsErasureRecordedAsync(document.StorageKey, cancellationToken).ConfigureAwait(false))
                    {
                        // Retention owns physical erasure and metadata cleanup. Do not make its
                        // recoverable deletion job skip an original that is still present.
                        document.FinishSafetyScan(leaseId, clock.GetUtcNow(), KycDocumentSafetyStatus.Blocked,
                            "Erasure ledger", "", null, "erasure-recorded");

                        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    await using var original = await storage.OpenAsync(document.StorageKey, cancellationToken).ConfigureAwait(false);
                    await using var verified = original is null ? null : await KycPrivateSpool.VerifyAsync(original, document.Length,
                        document.Sha256, storageOptions.Value.MaxFileBytes, cancellationToken).ConfigureAwait(false);

                    result = verified is null
                        ? new(KycScanVerdict.Blocked, "Integrity", "", null, "content-unavailable-or-changed")
                        : await scope.ServiceProvider.GetRequiredService<IKycDocumentScanner>().ScanAsync(verified, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is RequestFailedException or IOException or HttpRequestException)
                {
                    LogFailure(logger, exception);
                    result = new(KycScanVerdict.Unavailable, "Storage", "", null, "storage-unavailable");
                }
                var status = result.Verdict switch
                {
                    KycScanVerdict.Clean => KycDocumentSafetyStatus.Clean,
                    KycScanVerdict.Unsafe => KycDocumentSafetyStatus.Unsafe,
                    KycScanVerdict.Blocked => KycDocumentSafetyStatus.Blocked,
                    _ => KycDocumentSafetyStatus.Failed
                };

                now = clock.GetUtcNow();
                DateTimeOffset? retryAt = status == KycDocumentSafetyStatus.Failed && document.SafetyScanAttempts < settings.MaxAttempts
                    ? now.AddSeconds(Math.Min(900, 30 * Math.Pow(2, document.SafetyScanAttempts - 1))) : null;

                if (document.FinishSafetyScan(leaseId, now, status, result.Engine, result.Definitions,
                    result.DefinitionsAt, result.ReasonCode, retryAt))
                {
                    await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    completed++;
                }
            }
            catch (DbUpdateConcurrencyException exception)
            {
                // A different worker, reviewer retry or erasure owns the newer revision.
                LogConflict(logger, exception);
            }
        }
        return completed;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "KYC safety processing dependency unavailable.")]
    private static partial void LogFailure(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "KYC safety result superseded by another operation.")]
    private static partial void LogConflict(ILogger logger, Exception exception);
}
