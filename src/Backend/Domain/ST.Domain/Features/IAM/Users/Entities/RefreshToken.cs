using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Users.Entities;

public class RefreshToken : BaseEntity, ISoftDeletable
{

    public string AppUserId { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? TokenFamily { get; private set; } = Guid.CreateVersion7().ToString();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }



    // Methods

    public bool IsActiveAt(DateTimeOffset now) => !IsRevoked && !IsExpiredAt(now);

    [NotMapped]
    public bool IsUsed => UsedAt.HasValue;

    public bool IsExpiredAt(DateTimeOffset now) => now >= ExpiresAt;

    [NotMapped]
    public bool IsRevoked => RevokedAt.HasValue;

    private RefreshToken() { }

    public static RefreshToken Create(
        DateTimeOffset now,
        string appUserId,
        string token,
        DateTimeOffset expiresAt,
        string createdBy,
        string? createdByIp = null,
        string? tokenFamily = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            AppUserId = appUserId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            CreatedBy = createdBy,
            CreatedByIp = createdByIp,
            TokenFamily = string.IsNullOrWhiteSpace(tokenFamily)
                ? Guid.CreateVersion7().ToString()
                : tokenFamily
        };
    }

    public void MarkAsUsed(DateTimeOffset now)
    {
        UsedAt = now;
        SetUpdatedInfo(AppUserId, now);
    }

    public void Revoke(DateTimeOffset now, string reason, string? revokedByIp = null, string? replacedByToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RevokedAt = now;
        RevokedReason = reason;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        SetUpdatedInfo(AppUserId, now);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;

        if (!IsRevoked)
        {
            RevokedAt = DeletedAt;
            RevokedReason = "Soft deleted";
        }

        SetUpdatedInfo(deletedBy, now);
    }

    // Navigation

    [JsonIgnore]
    public virtual AppUser AppUser { get; set; } = null!;
}
