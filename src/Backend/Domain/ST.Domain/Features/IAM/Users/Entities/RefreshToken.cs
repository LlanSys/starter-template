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

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;

    [NotMapped]
    public bool IsUsed => UsedAt.HasValue;

    [NotMapped]
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsRevoked => RevokedAt.HasValue;

    private RefreshToken() { }

    public static RefreshToken Create(
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
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            CreatedByIp = createdByIp,
            TokenFamily = string.IsNullOrWhiteSpace(tokenFamily)
                ? Guid.CreateVersion7().ToString()
                : tokenFamily
        };
    }

    public void MarkAsUsed()
    {
        UsedAt = DateTimeOffset.UtcNow;
        SetUpdatedInfo(AppUserId);
    }

    public void Revoke(string reason, string? revokedByIp = null, string? replacedByToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        SetUpdatedInfo(AppUserId);
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;

        if (!IsRevoked)
        {
            RevokedAt = DeletedAt;
            RevokedReason = "Soft deleted";
        }

        SetUpdatedInfo(deletedBy);
    }

    // Navigation

    [JsonIgnore]
    public virtual AppUser AppUser { get; set; } = null!;
}
