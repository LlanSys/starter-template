using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Entities;

/// <summary>
/// Child entity of Customer. Has its own identity — can be added/removed independently.
/// Corresponds to Directors' Details tab in the original UI.
/// </summary>
public class Director : BaseEntity, ISoftDeletable
{
    public Guid CustomerId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public DirectorRelationType RelationType { get; private set; }
    public IdentificationType IdentificationType { get; private set; }
    public string IdentificationNumber { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public decimal? SharePercentage { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // EF Core
    private Director() { }

    public static Director Create(
        Guid customerId,
        string fullName,
        DirectorRelationType relationType,
        IdentificationType identificationType,
        string identificationNumber,
        string createdBy,
        string? phoneNumber = null,
        string? email = null,
        decimal? sharePercentage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        if (sharePercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(sharePercentage), "Share percentage must be between 0 and 100.");

        return new Director
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            FullName = fullName.Trim(),
            RelationType = relationType,
            IdentificationType = identificationType,
            IdentificationNumber = identificationNumber.Trim().ToUpper(CultureInfo.CurrentCulture),
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim().ToLower(CultureInfo.CurrentCulture),
            SharePercentage = sharePercentage,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy

        };
    }

    internal void Update(
        string fullName,
        DirectorRelationType relationType,
        IdentificationType identificationType,
        string identificationNumber,
        string? phoneNumber = null,
        string? email = null,
        decimal? sharePercentage = null)
    {
        FullName = fullName.Trim();
        RelationType = relationType;
        IdentificationType = identificationType;
        IdentificationNumber = identificationNumber.Trim().ToUpper(CultureInfo.CurrentCulture);
        PhoneNumber = phoneNumber?.Trim();
        Email = email?.Trim().ToLower(CultureInfo.CurrentCulture);
        SharePercentage = sharePercentage;
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentNullException.ThrowIfNull(deletedBy);
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;


    }
}
