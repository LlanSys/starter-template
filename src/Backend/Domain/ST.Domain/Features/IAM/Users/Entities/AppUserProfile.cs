using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Users.Entities;

public class AppUserProfile : BaseEntity, ISoftDeletable
{
    public string? AppUserId { get; private set; }
    public string? TelephoneNo { get; private set; }
    public string? MobileNo { get; private set; }
    public string? Email { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private AppUserProfile() { }

    public static AppUserProfile Create(string appUserId, string? telephoneNo, string? mobileNo, string? email, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUserProfile
        {
            Id = Guid.CreateVersion7(),
            AppUserId = appUserId,
            TelephoneNo = telephoneNo?.Trim(),
            MobileNo = mobileNo?.Trim(),
            Email = email?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateContact(string? telephoneNo, string? mobileNo, string? email, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        TelephoneNo = telephoneNo?.Trim();
        MobileNo = mobileNo?.Trim();
        Email = email?.Trim();
        SetUpdatedInfo(updatedBy);
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);
        DeletedBy = deletedBy;
        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
    }
}
