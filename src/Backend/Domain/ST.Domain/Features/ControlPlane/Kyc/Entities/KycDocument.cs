using ST.Domain.Features.ControlPlane.Kyc.Enums;

namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycDocument
{
    private KycDocument()
    {
    }

    public KycDocument(Guid tenantId, string documentType, string fileName, string contentType,
        long length, string sha256, string uploadedBy, DateTimeOffset now, string documentTypeKey = "legacy")
    {
        TenantId = tenantId;
        DocumentType = documentType;
        DocumentTypeKey = documentTypeKey;
        FileName = fileName;
        ContentType = contentType;
        Length = length;
        Sha256 = sha256;
        UploadedBy = uploadedBy;
        UploadedAt = now;
        NextSafetyScanAt = now;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid TenantId
    {
        get; private set;
    }
    public string DocumentTypeKey { get; private set; } = "legacy";
    public string DocumentType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Length
    {
        get; private set;
    }
    public string Sha256 { get; private set; } = string.Empty;
    public string UploadedBy { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt
    {
        get; private set;
    }
    public string Status { get; private set; } = "Awaiting review";
    public Guid Revision { get; private set; } = Guid.CreateVersion7();
    public DateTimeOffset? ErasedAt
    {
        get; private set;
    }
    public KycDocumentSafetyStatus SafetyStatus
    {
        get; private set;
    }
    public Guid? SafetyLeaseId
    {
        get; private set;
    }
    public DateTimeOffset? SafetyLeaseUntil
    {
        get; private set;
    }
    public int SafetyScanAttempts
    {
        get; private set;
    }
    public DateTimeOffset? NextSafetyScanAt { get; private set; } = DateTimeOffset.UnixEpoch;
    public DateTimeOffset? SafetyCheckedAt
    {
        get; private set;
    }
    public string? SafetyEngine
    {
        get; private set;
    }
    public string? SafetyDefinitions
    {
        get; private set;
    }
    public DateTimeOffset? SafetyDefinitionsAt
    {
        get; private set;
    }
    public string? SafetyReasonCode
    {
        get; private set;
    }

    public bool TryBeginSafetyScan(Guid leaseId, DateTimeOffset now, TimeSpan lease, int maxAttempts)
    {
        var eligible = !ErasedAt.HasValue && (SafetyStatus == KycDocumentSafetyStatus.Scanning
            ? !SafetyLeaseUntil.HasValue || SafetyLeaseUntil <= now
            : SafetyStatus is KycDocumentSafetyStatus.Pending or KycDocumentSafetyStatus.Failed
                && NextSafetyScanAt.HasValue && NextSafetyScanAt <= now);

        if (!eligible)
        {
            return false;
        }

        if (SafetyScanAttempts >= maxAttempts)
        {
            SafetyStatus = KycDocumentSafetyStatus.Failed;
            SafetyReasonCode = "attempt-limit";
            NextSafetyScanAt = null;
            SafetyLeaseId = null;
            SafetyLeaseUntil = null;
            Revision = Guid.CreateVersion7();
            return false;
        }
        SafetyStatus = KycDocumentSafetyStatus.Scanning;
        SafetyLeaseId = leaseId;
        SafetyLeaseUntil = now.Add(lease);
        SafetyScanAttempts++;
        Revision = Guid.CreateVersion7();
        return true;
    }

    public void BlockChangedContent(DateTimeOffset now)
    {
        if (ErasedAt.HasValue || SafetyStatus != KycDocumentSafetyStatus.Clean)
        {
            return;
        }

        SafetyStatus = KycDocumentSafetyStatus.Blocked;
        SafetyReasonCode = "content-unavailable-or-changed";
        SafetyCheckedAt = now;
        SafetyEngine = "Integrity";
        SafetyDefinitions = null;
        SafetyDefinitionsAt = null;
        Revision = Guid.CreateVersion7();
    }

    public void QueueSafetyScan(DateTimeOffset now)
    {
        if (ErasedAt.HasValue)
        {
            throw new InvalidOperationException("Erased evidence cannot be scanned.");
        }

        SafetyStatus = KycDocumentSafetyStatus.Pending;
        SafetyLeaseId = null;
        SafetyLeaseUntil = null;
        SafetyScanAttempts = 0;
        NextSafetyScanAt = now;
        SafetyCheckedAt = null;
        SafetyEngine = null;
        SafetyDefinitions = null;
        SafetyDefinitionsAt = null;
        SafetyReasonCode = null;
        Revision = Guid.CreateVersion7();
    }

    public bool FinishSafetyScan(Guid leaseId, DateTimeOffset now, KycDocumentSafetyStatus status,
        string engine, string definitions, DateTimeOffset? definitionsAt, string reasonCode,
        DateTimeOffset? retryAt = null)
    {
        if (ErasedAt.HasValue || SafetyStatus != KycDocumentSafetyStatus.Scanning
            || SafetyLeaseId != leaseId || !SafetyLeaseUntil.HasValue || SafetyLeaseUntil <= now)
        {
            return false;
        }

        if (status is not (KycDocumentSafetyStatus.Clean or KycDocumentSafetyStatus.Unsafe
            or KycDocumentSafetyStatus.Blocked or KycDocumentSafetyStatus.Failed))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        SafetyStatus = status;
        SafetyCheckedAt = now;
        SafetyEngine = engine;
        SafetyDefinitions = definitions;
        SafetyDefinitionsAt = definitionsAt;
        SafetyReasonCode = reasonCode;
        SafetyLeaseId = null;
        SafetyLeaseUntil = null;
        NextSafetyScanAt = status == KycDocumentSafetyStatus.Failed ? retryAt : null;
        Revision = Guid.CreateVersion7();
        return true;
    }

    public string StorageKey => $"kyc/{TenantId:N}/{Id:N}";

    public void EraseMetadata(DateTimeOffset now, string documentTypeKey = "legacy")
    {
        ErasedAt = now;
        SafetyStatus = KycDocumentSafetyStatus.Erased;
        SafetyLeaseId = null;
        SafetyLeaseUntil = null;
        NextSafetyScanAt = null;
        SafetyEngine = null;
        SafetyDefinitions = null;
        SafetyDefinitionsAt = null;
        SafetyReasonCode = null;
        FileName = "Deleted document";
        DocumentType = string.Empty;
        DocumentTypeKey = string.Empty;
        ContentType = "application/octet-stream";
        Length = 0;
        Sha256 = Id.ToString("N").PadRight(64, '0');
        UploadedBy = "00000000-0000-0000-0000-000000000001";
        Status = "Erased";
        Revision = Guid.CreateVersion7();
    }

    public void Review(bool accepted)
    {
        if (accepted && SafetyStatus != KycDocumentSafetyStatus.Clean)
        {
            throw new InvalidOperationException("Evidence must pass safety checks before acceptance.");
        }

        Status = accepted ? "Accepted" : "Changes requested";
        Revision = Guid.CreateVersion7();
    }
}
