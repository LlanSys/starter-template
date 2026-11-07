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
        DateTimeOffset now,
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

    public void RefreshAccess(DateTimeOffset now, DateTimeOffset expiresAt, string ipAddress, string userAgent)
    {
        LastAccessedAt = now;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        SetUpdatedInfo(AppUserId, now);
    }

    public void Touch(DateTimeOffset now, DateTimeOffset? expiresAt = null)
    {
        LastAccessedAt = now;
        if (expiresAt.HasValue)
        {
            ExpiresAt = expiresAt.Value;
        }

        SetUpdatedInfo(AppUserId, now);
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsActive = false;
        IsRevoked = true;
        EndedAt = now;
        EndReason = reason;
        SetUpdatedInfo(AppUserId, now);
    }

    public void Expire(DateTimeOffset now)
    {
        IsActive = false;
        EndedAt = now;
        EndReason = "Session expired";
        SetUpdatedInfo(AppUserId, now);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        IsActive = false;
        EndedAt ??= now;
        EndReason ??= "Soft deleted";
        SetUpdatedInfo(deletedBy, now);
    }

    // Navigation
    [JsonIgnore]
    public virtual AppUser AppUser { get; set; } = null!;
}
