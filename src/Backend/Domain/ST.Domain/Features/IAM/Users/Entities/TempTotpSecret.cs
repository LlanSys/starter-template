using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Users.Entities;

public class TempTotpSecret : BaseEntity, ISoftDeletable
{
    public string UserId { get; private set; } = string.Empty;
    public string EncryptedSecret { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }

    public virtual AppUser User { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private TempTotpSecret() { }

    public static TempTotpSecret Create(string userId, string encryptedSecret, DateTimeOffset expiresAt, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new TempTotpSecret
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            EncryptedSecret = encryptedSecret,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentNullException.ThrowIfNull(deletedBy);
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
