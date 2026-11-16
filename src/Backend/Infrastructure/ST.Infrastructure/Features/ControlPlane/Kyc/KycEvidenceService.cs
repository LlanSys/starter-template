using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Azure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Kyc.Enums;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.SharedKernel.Features.ControlPlane.Kyc;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class KycEvidenceService(
    IControlPlaneUnitOfWork unit,
    IKycDocumentStorage storage,
    IKycEvidenceLifecycleStorage lifecycleStorage,
    IKycRetentionService retention,
    ICurrentActorProvider actor,
    TimeProvider clock,
    IOptions<KycDocumentStorageSettings> configuration,
    IValidator<ReviewKycDocumentRequest> reviewValidator,
    ILogger<KycEvidenceService> logger) : IKycEvidenceService
{
    private readonly KycDocumentStorageSettings _settings = configuration.Value;
    private const string LinkUnavailable = "This submission link is invalid, expired, or closed.";

    public async Task<AppResponse<KycCasePageResponse>> CasesAsync(string? search, int page, CancellationToken cancellationToken)
    {
        if (page < 0 || page > 100000 || search?.Length > 80)
        {
            return AppResponses.Failure<KycCasePageResponse>("Enter a shorter search and a valid page.");
        }

        var query = unit.Tenants.FindAll().Where(tenant => !tenant.IsDemo && tenant.Id != Tenant.PlatformTenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(tenant => tenant.DisplayName.ToUpper().Contains(normalized));
        }
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderBy(tenant => tenant.KycApprovedAt.HasValue).ThenBy(tenant => tenant.DisplayName).ThenBy(tenant => tenant.Id)
            .Skip(page * 50).Take(50).Select(tenant => new KycCaseSummaryResponse(tenant.Id, tenant.DisplayName, tenant.KycApprovedAt))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success(new KycCasePageResponse(items, total, page, 50));
    }

    public async Task<AppResponse<KycCaseSummaryResponse>> CaseAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var item = await unit.Tenants.FindAll().Where(tenant => tenant.Id == tenantId
                && !tenant.IsDemo && tenant.Id != Tenant.PlatformTenantId)
            .Select(tenant => new KycCaseSummaryResponse(tenant.Id, tenant.DisplayName, tenant.KycApprovedAt))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return item is null ? AppResponses.NotFound<KycCaseSummaryResponse>("Workspace not found.") : AppResponses.Success(item with
        {
            DocumentTypes = _settings.DocumentTypes
        });
    }

    public async Task<AppResponse<IReadOnlyList<KycDocumentResponse>>> ListAsync(Guid tenantId, CancellationToken cancellationToken, string? documentTypeKey = null)
    {
        if (!await unit.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.IsDemo == false
            && tenant.Id != Tenant.PlatformTenantId, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.NotFound<IReadOnlyList<KycDocumentResponse>>("Workspace not found.");
        }

        if (await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Success<IReadOnlyList<KycDocumentResponse>>([]);
        }

        if (documentTypeKey?.Length > 64)
        {
            return AppResponses.Failure<IReadOnlyList<KycDocumentResponse>>("Choose a valid document type.");
        }
        var key = string.IsNullOrWhiteSpace(documentTypeKey) ? null : documentTypeKey.Trim().ToLowerInvariant();
        var documents = await unit.KycDocuments.FindAll().Where(item => item.TenantId == tenantId
                && (key == null || item.DocumentTypeKey == key))
            .OrderByDescending(item => item.UploadedAt).Take(200).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var ids = documents.Select(item => item.Id).ToArray();
        var reviews = await unit.KycDocumentReviews.FindAll().Where(item => ids.Contains(item.DocumentId))
            .OrderByDescending(item => item.ReviewedAt).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var grouped = reviews.ToLookup(item => item.DocumentId);
        IReadOnlyList<KycDocumentResponse> result = documents.Select(item => new KycDocumentResponse(
            item.Id, item.DocumentType, item.FileName, item.ContentType, item.Length, item.UploadedAt,
            item.UploadedBy, item.Status, item.Sha256,
            grouped[item.Id].Select(review => new KycReviewResponse(review.Accepted, review.Feedback,
                review.ReviewedBy, review.ReviewedAt)).ToArray())
        {
            DocumentTypeKey = item.DocumentTypeKey,
            SafetyStatus = item.SafetyStatus.ToString(),
            SafetyCheckedAt = item.SafetyCheckedAt,
            Revision = item.Revision
        }).ToArray();

        return AppResponses.Success(result);
    }

    public async Task<AppResponse<KycSubmissionLinkResponse>> IssueLinkAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null || !tenant.IsCustomerWorkspace || tenant.KycApprovedAt.HasValue)
        {
            return AppResponses.Failure<KycSubmissionLinkResponse>("A submission link requires a customer workspace awaiting KYC.");
        }

        var retentionCase = await retention.EnsureAsync(tenantId, false, cancellationToken).ConfigureAwait(false);

        if (!retentionCase.CanCollect || await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<KycSubmissionLinkResponse>("This KYC case is closed.");
        }

        retentionCase.Touch(clock.GetUtcNow());
        var link = await unit.KycSubmissionLinks.FindAll().AsTracking()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

        if (link is null)
        {
            link = new KycSubmissionLink(tenantId);
            await unit.KycSubmissionLinks.CreateAsync(link, cancellationToken).ConfigureAwait(false);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var now = clock.GetUtcNow();
        link.Issue(HashToken(token), actor.ActorId, now, TimeSpan.FromHours(_settings.SubmissionLinkHours),
            _settings.SubmissionUploadLimit);

        try
        {
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success(new KycSubmissionLinkResponse(token, link.ExpiresAt));
        }
        catch (DbUpdateException exception)
        {
            LogFailure(logger, "issue submission link", exception);
            return AppResponses.Failure<KycSubmissionLinkResponse>(AppError.BusinessRule("The submission link changed. Refresh and try again."));
        }
    }

    public async Task<AppResponse<bool>> RevokeLinkAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var revision = Guid.CreateVersion7();
        await unit.KycSubmissionLinks.FindAll().Where(item => item.TenantId == tenantId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.RevokedAt, now)
                .SetProperty(item => item.Revision, revision), cancellationToken).ConfigureAwait(false);

        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }

    public async Task<AppResponse<KycSubmissionStatusResponse>> InspectLinkAsync(Guid tenantId, string token, CancellationToken cancellationToken)
    {
        var link = await FindValidLinkAsync(tenantId, token, cancellationToken).ConfigureAwait(false);

        if (link is null)
        {
            return AppResponses.Failure<KycSubmissionStatusResponse>(LinkUnavailable);
        }

        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var documents = await unit.KycDocuments.FindAll()
            .Where(item => item.TenantId == tenantId && item.UploadedBy == link.SubmitterId)
            .OrderByDescending(item => item.UploadedAt).Take(100).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var ids = documents.Select(item => item.Id).ToArray();
        var reviews = await unit.KycDocumentReviews.FindAll().Where(item => ids.Contains(item.DocumentId))
            .OrderByDescending(item => item.ReviewedAt).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var grouped = reviews.ToLookup(item => item.DocumentId);
        return AppResponses.Success(new KycSubmissionStatusResponse(tenant!.DisplayName, link.ExpiresAt,
            _settings.MaxFileBytes, Math.Max(0, link.UploadLimit - link.Uploads),
            documents.Select(item => new KycSubmittedDocumentResponse(item.DocumentType, item.FileName,
                item.UploadedAt, item.Status, grouped[item.Id].FirstOrDefault()?.Feedback)
            {
                DocumentTypeKey = item.DocumentTypeKey,
                SafetyStatus = item.SafetyStatus.ToString(),
                SafetyCheckedAt = item.SafetyCheckedAt
            }).ToArray())
        {
            DocumentTypes = _settings.DocumentTypes
        });
    }

    public async Task<AppResponse<Guid>> UploadAsync(Guid tenantId, string? token, string documentType,
        string fileName, long length, Stream content, CancellationToken cancellationToken, string? otherDocumentName = null)
    {
        var type = _settings.DocumentTypes.FirstOrDefault(item => string.Equals(item.Key, documentType?.Trim(), StringComparison.OrdinalIgnoreCase));
        var isOther = string.Equals(documentType?.Trim(), KycDocumentTypes.Other, StringComparison.OrdinalIgnoreCase);
        var documentName = isOther ? otherDocumentName?.Trim() : type?.Name.Trim();

        if ((!isOther && type is null) || string.IsNullOrWhiteSpace(documentName) || documentName.Length > 80
            || documentName.Any(char.IsControl) || (!isOther && !string.IsNullOrEmpty(otherDocumentName)))
        {
            return AppResponses.Failure<Guid>("Choose a listed document type, or choose Other and enter its name (at most 80 characters).");
        }
        var typeKey = isOther ? KycDocumentTypes.Other : type!.Key;
        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null || !tenant.IsCustomerWorkspace)
        {
            return AppResponses.NotFound<Guid>("Workspace not found.");
        }

        var retentionCase = await retention.EnsureAsync(tenantId, tenant.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);

        if (!retentionCase.CanCollect || await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<Guid>("This KYC case is closed.");
        }

        KycSubmissionLink? link = null;

        if (token is not null)
        {
            link = await FindValidLinkAsync(tenantId, token, cancellationToken).ConfigureAwait(false);

            if (link is null || link.Uploads >= link.UploadLimit)
            {
                return AppResponses.Failure<Guid>(LinkUnavailable);
            }
        }

        if (length <= 0 || length > _settings.MaxFileBytes || string.IsNullOrWhiteSpace(documentType)
            || documentType.Trim().Length > 64)
        {
            return AppResponses.Failure<Guid>("Choose a document type and a nonempty file within the upload size limit.");
        }

        var safeName = fileName.Replace((char)92, '/').Split('/').Last();

        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 180 || safeName.Any(char.IsControl))
        {
            return AppResponses.Failure<Guid>("The document filename is invalid.");
        }

        if (await unit.KycDocuments.FindAll().CountAsync(item => item.TenantId == tenantId, cancellationToken).ConfigureAwait(false) >= 200)
        {
            return AppResponses.Failure<Guid>("This workspace has reached its document limit. Contact your onboarding team.");
        }

        // A private, delete-on-close spool bounds memory and permits validation before blob persistence.
        var tempPath = Path.Combine(Path.GetTempPath(), $"st-kyc-{Guid.CreateVersion7():N}");
        var spoolOptions = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            BufferSize = 81920,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
        };

        if (!OperatingSystem.IsWindows())
        {
            spoolOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using var spool = new FileStream(tempPath, spoolOptions);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);

        try
        {
            long actualLength = 0;
            int read;

            while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                actualLength += read;

                if (actualLength > _settings.MaxFileBytes || actualLength > length)
                {
                    return AppResponses.Failure<Guid>("The file exceeds the upload size limit.");
                }

                await spool.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            if (actualLength != length)
            {
                return AppResponses.Failure<Guid>("The uploaded file is incomplete.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        spool.Position = 0;
        var signature = new byte[8];
        var signatureLength = (int)Math.Min(signature.Length, spool.Length);
        await spool.ReadExactlyAsync(signature.AsMemory(0, signatureLength), cancellationToken).ConfigureAwait(false);
        var contentType = DetectContentType(signature.AsSpan(0, signatureLength), Path.GetExtension(safeName));

        if (contentType is null)
        {
            return AppResponses.Failure<Guid>("Upload a PDF, JPEG, or PNG file with matching file contents.");
        }

        spool.Position = 0;
        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(spool, cancellationToken).ConfigureAwait(false));
        var uploadedBy = link?.SubmitterId ?? actor.ActorId;
        var existing = await unit.KycDocuments.FindAll()
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Sha256 == checksum
                && item.UploadedBy == uploadedBy && item.DocumentTypeKey == typeKey
                && item.DocumentType == documentName, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return AppResponses.Success(existing.Id);
        }

        var document = new KycDocument(tenantId, documentName, safeName, contentType,
            length, checksum, uploadedBy, clock.GetUtcNow(), typeKey);

        var reservation = new KycUploadReservation { Id = document.Id, TenantId = tenantId, LeaseUntil = clock.GetUtcNow().AddMinutes(15) };
        await unit.KycUploadReservations.CreateAsync(reservation, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        spool.Position = 0;
        var blobSaved = false;

        try
        {
            await storage.SaveAsync(document.StorageKey, spool, contentType, cancellationToken).ConfigureAwait(false);
            blobSaved = true;

            if (link is not null)
            {
                // Fences a concurrent link rotation, revocation, or another upload against the original revision.
                if (!link.IsAvailable(clock.GetUtcNow())
                    || !await unit.Tenants.AnyAsync(item => item.Id == tenantId && item.KycApprovedAt == null, cancellationToken).ConfigureAwait(false))
                {
                    await CleanupAsync(document.StorageKey).ConfigureAwait(false);
                    return AppResponses.Failure<Guid>(LinkUnavailable);
                }

                link.RecordUpload();
            }

            retentionCase.Touch(clock.GetUtcNow());
            var pinnedPolicy = await unit.KycRetentionPolicies.FindAll().SingleAsync(x => x.Id == retentionCase.PolicyId, cancellationToken).ConfigureAwait(false);
            retentionCase.Schedule(pinnedPolicy, tenant.KycApprovedAt.HasValue);
            reservation.State = "Completed";
            reservation.Revision = Guid.CreateVersion7();
            await unit.KycDocuments.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success(document.Id);
        }
        catch (DbUpdateException exception)
        {
            if (blobSaved)
            {
                await CleanupAsync(document.StorageKey).ConfigureAwait(false);
            }

            LogFailure(logger, "save metadata", exception);
            return AppResponses.Failure<Guid>("The document changed during upload. Refresh and try again.");
        }
        catch (Exception exception) when (exception is RequestFailedException or HttpRequestException or IOException or OperationCanceledException)
        {
            if (blobSaved)
            {
                await CleanupAsync(document.StorageKey).ConfigureAwait(false);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            LogFailure(logger, "upload", exception);
            return AppResponses.Failure<Guid>(AppError.DependencyUnavailable("Document storage is temporarily unavailable."));
        }
    }

    public async Task<AppResponse<bool>> RetrySafetyScanAsync(Guid tenantId, Guid documentId, RetryKycSafetyScanRequest request, CancellationToken cancellationToken)
    {
        var document = await unit.KycDocuments.FindAll().AsTracking().SingleOrDefaultAsync(item => item.Id == documentId
            && item.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

        if (document is null)
        {
            return AppResponses.NotFound<bool>("Document not found.");
        }

        if (document.Revision != request.Revision || request.Revision == Guid.Empty)
        {
            return AppResponses.Failure<bool>("The document changed. Refresh and try again.");
        }

        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var retentionCase = await retention.EnsureAsync(tenantId, tenant!.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);

        if (!retentionCase.CanCollect || document.ErasedAt.HasValue
            || document.SafetyStatus is KycDocumentSafetyStatus.Clean or KycDocumentSafetyStatus.Pending or KycDocumentSafetyStatus.Scanning
            || await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false)
            || await lifecycleStorage.IsErasureRecordedAsync(document.StorageKey, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<bool>("This document is unavailable for a safety retry.");
        }

        document.QueueSafetyScan(clock.GetUtcNow());

        try
        {
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            LogSafetyRetry(logger, actor.ActorId, document.Id);
            return AppResponses.Success(true);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            LogFailure(logger, "retry safety scan", exception);
            return AppResponses.Failure<bool>("The document changed. Refresh and try again.");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Actor {ActorId} requested a KYC safety retry for document {DocumentId}.")]
    private static partial void LogSafetyRetry(ILogger logger, string actorId, Guid documentId);

    public async Task<AppResponse<bool>> ReviewAsync(Guid tenantId, Guid documentId, ReviewKycDocumentRequest request, CancellationToken cancellationToken)
    {
        var validation = await reviewValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid)
        {
            return AppResponses.Failure<bool>("Enter review feedback of at most 2000 characters.");
        }

        var document = await unit.KycDocuments.FindAll().AsTracking()
            .SingleOrDefaultAsync(item => item.Id == documentId && item.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

        if (document is null)
        {
            return AppResponses.NotFound<bool>("Document not found.");
        }

        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var retentionCase = await retention.EnsureAsync(tenantId, tenant!.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);

        if (!retentionCase.CanCollect || document.ErasedAt.HasValue
            || await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<bool>("This evidence is closed for review.");
        }

        retentionCase.Touch(clock.GetUtcNow());

        if (request.Accepted && document.SafetyStatus != KycDocumentSafetyStatus.Clean)
        {
            return AppResponses.Failure<bool>("The document must pass safety checks before it can be accepted.");
        }

        document.Review(request.Accepted);
        await unit.KycDocumentReviews.CreateAsync(new KycDocumentReview(documentId, request.Accepted,
            request.Feedback.Trim(), actor.ActorId, clock.GetUtcNow()), cancellationToken).ConfigureAwait(false);

        try
        {
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success(true);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            LogFailure(logger, "review document", exception);
            return AppResponses.Failure<bool>(AppError.BusinessRule("The document review changed. Refresh and try again."));
        }
    }

    public async Task<AppResponse<KycDocumentDownload>> DownloadAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await unit.KycDocuments.FindAll().AsTracking().SingleOrDefaultAsync(
            item => item.Id == documentId && item.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

        if (document is null)
        {
            return AppResponses.NotFound<KycDocumentDownload>("Document not found.");
        }

        if (document.SafetyStatus != KycDocumentSafetyStatus.Clean)
        {
            return AppResponses.Failure<KycDocumentDownload>("This document is unavailable until safety checks pass.");
        }

        var retentionCase = await retention.EnsureAsync(tenantId, true, cancellationToken).ConfigureAwait(false);

        if (document.ErasedAt.HasValue || retentionCase.Phase != "Retained")
        {
            return AppResponses.NotFound<KycDocumentDownload>("Document unavailable under the retention schedule.");
        }

        try
        {
            if (await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false)
                || await lifecycleStorage.IsErasureRecordedAsync(document.StorageKey, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.NotFound<KycDocumentDownload>("This document has been erased.");
            }

            await using var original = await storage.OpenAsync(document.StorageKey, cancellationToken).ConfigureAwait(false);
            var verified = original is null ? null : await KycPrivateSpool.VerifyAsync(original, document.Length,
                document.Sha256, _settings.MaxFileBytes, cancellationToken).ConfigureAwait(false);

            if (verified is not null)
            {
                return AppResponses.Success(new KycDocumentDownload(verified, document.ContentType, document.FileName));
            }

            document.BlockChangedContent(clock.GetUtcNow());

            try
            {
                await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                // A newer erasure/review/scan revision wins; this download remains denied.
                LogFailure(logger, "record changed original", exception);
            }
            return AppResponses.Failure<KycDocumentDownload>("The document could not be verified. Ask your reviewer to retry its safety check.");
        }
        catch (Exception exception) when (exception is RequestFailedException or HttpRequestException or IOException)
        {
            LogFailure(logger, "download", exception);
            return AppResponses.Failure<KycDocumentDownload>(AppError.DependencyUnavailable("Document storage is temporarily unavailable."));
        }
    }

    private async Task<KycSubmissionLink?> FindValidLinkAsync(Guid tenantId, string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64)
        {
            return null;
        }

        if (!await unit.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.KycApprovedAt == null
            && !tenant.IsDemo && tenant.Id != Tenant.PlatformTenantId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        if (await unit.KycRetentionCases.FindAll().AnyAsync(x => x.TenantId == tenantId
            && (x.Outcome != "Open" || x.Phase != "Retained"), cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        if (await lifecycleStorage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var hash = HashToken(token);
        var now = clock.GetUtcNow();
        return await unit.KycSubmissionLinks.FindAll().AsTracking()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.TokenHash == hash
                && item.RevokedAt == null && item.ExpiresAt > now, cancellationToken).ConfigureAwait(false);
    }

    private async Task CleanupAsync(string key)
    {
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10), clock);

        try
        {
            await storage.DeleteAsync(key, cleanupTimeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is RequestFailedException or HttpRequestException or IOException or OperationCanceledException)
        {
            LogFailure(logger, "cleanup unattached upload", exception);
        }
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string? DetectContentType(ReadOnlySpan<byte> signature, string extension)
    {
        if (signature.StartsWith("%PDF-"u8) && extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "application/pdf";
        }

        if (signature.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (signature.Length >= 3 && signature[0] == 255 && signature[1] == 216 && signature[2] == 255
            && (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
        {
            return "image/jpeg";
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "KYC document operation {Operation} failed.")]
    private static partial void LogFailure(ILogger logger, string operation, Exception exception);
}
