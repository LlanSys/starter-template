namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycSubmissionLink
{
    private KycSubmissionLink() { }

    public KycSubmissionLink(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A workspace is required.", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    public Guid TenantId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string SubmitterId => $"kyc-link:{SubmissionId:N}";
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string IssuedBy { get; private set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; private set; }
    public int Uploads { get; private set; }
    public int UploadLimit { get; private set; }
    public Guid Revision { get; private set; } = Guid.CreateVersion7();

    public void Issue(string hash, string actorId, DateTimeOffset now, TimeSpan lifetime, int uploadLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (hash.Length != 64 || lifetime <= TimeSpan.Zero || uploadLimit <= 0)
        {
            throw new ArgumentException("A valid submission hash, lifetime, and upload limit are required.");
        }

        SubmissionId = Guid.CreateVersion7();
        TokenHash = hash;
        IssuedBy = actorId;
        IssuedAt = now;
        ExpiresAt = now.Add(lifetime);
        UploadLimit = uploadLimit;
        Uploads = 0;
        RevokedAt = null;
        Revision = Guid.CreateVersion7();
    }

    public bool IsAvailable(DateTimeOffset now)
    {
        return RevokedAt is null && ExpiresAt > now;
    }

    public void RecordUpload()
    {
        if (Uploads >= UploadLimit)
        {
            throw new InvalidOperationException("The submission upload limit has been reached.");
        }

        Uploads++;
        Revision = Guid.CreateVersion7();
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
        Revision = Guid.CreateVersion7();
    }
}
