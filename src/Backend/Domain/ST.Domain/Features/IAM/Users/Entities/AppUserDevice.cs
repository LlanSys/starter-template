using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Users.Entities;

public class AppUserDevice : BaseEntity, ISoftDeletable
{
    [MaxLength(450)]
    public string AppUserId { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsTrusted { get; set; }
    public DateTimeOffset? TrustedUntil { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        SetUpdatedInfo(deletedBy, now);
    }

    public void RevokeTrust(DateTimeOffset now, string revokedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedBy);

        IsTrusted = false;
        TrustedUntil = null;
        SetUpdatedInfo(revokedBy, now);
    }

    [JsonIgnore]
    public virtual AppUser AppUser { get; set; } = null!;
}
