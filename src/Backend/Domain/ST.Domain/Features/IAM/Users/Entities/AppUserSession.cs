using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Users.Entities;

public class AppUserSession : BaseEntity, ISoftDeletable
{
    public string AppUserId { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public DateTimeOffset LastAccessedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public string? EndReason { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsRevoked { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    [Required]
    [MaxLength(256)] // SHA-256 hash is 64 hex characters, so 256 is safe.
    public string DeviceFingerprint { get; private set; } = string.Empty;

    private AppUserSession() { }

    public static AppUserSession Create(
        Guid id,
        string appUserId,
        string ipAddress,
        string userAgent,
        DateTimeOffset expiresAt,
        string createdBy,
        string? deviceFingerprint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var now = DateTimeOffset.UtcNow;
        return new AppUserSession
        {
            Id = id,
            AppUserId = appUserId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint ?? string.Empty,
            CreatedAt = now,
            CreatedBy = createdBy,
            LastAccessedAt = now,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public void RefreshAccess(DateTimeOffset expiresAt, string ipAddress, string userAgent)
    {
        LastAccessedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        SetUpdatedInfo(AppUserId);
    }

    public void Touch(DateTimeOffset? expiresAt = null)
    {
        LastAccessedAt = DateTimeOffset.UtcNow;
        if (expiresAt.HasValue)
        {
            ExpiresAt = expiresAt.Value;
        }

        SetUpdatedInfo(AppUserId);
    }

    public void Revoke(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsActive = false;
        IsRevoked = true;
        EndedAt = DateTimeOffset.UtcNow;
        EndReason = reason;
        SetUpdatedInfo(AppUserId);
    }

    public void Expire()
    {
        IsActive = false;
        EndedAt = DateTimeOffset.UtcNow;
        EndReason = "Session expired";
        SetUpdatedInfo(AppUserId);
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
        EndedAt ??= DateTimeOffset.UtcNow;
        EndReason ??= "Soft deleted";
        SetUpdatedInfo(deletedBy);
    }

    // Navigation
    [JsonIgnore]
    public virtual AppUser AppUser { get; set; } = null!;
}
