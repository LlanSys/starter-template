using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.HR.Employees.Enums;
using ST.Domain.Features.Banking.Customers.Events;
using ST.Domain.Features.HR.Employees.Events;
using ST.Domain.Features.IAM.Users.Events;
using ST.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace ST.Domain.Features.IAM.Users.Entities;

public class AppUser : IdentityUser, ISoftDeletable, IHasDomainEvents
{
    public Guid TenantId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CustomerId { get; set; }

    // Belongs here because it's the KYC anchor, not HR or CRM data
    public string NationalId { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;

    // Minimal personal data needed for auth UX only
    // Source of truth for these remains Employee/Customer
    // These are a CACHE of display data
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public Uri? ProfilePictureUrl { get; set; }


    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LastFailedLoginAt { get; set; }
    public bool RequirePasswordChange { get; set; }
    public DateTimeOffset? PasswordLastChanged { get; set; }

    public string? TotpSecret { get; set; }

    public bool IsActive { get; set; }


    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public string? ActivatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public string? DeactivatedBy { get; set; }
    public string? DeactivationReason { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];



    [JsonIgnore]
    public virtual ICollection<RefreshToken> RefreshTokens { get; } = [];

    [JsonIgnore]
    public virtual ICollection<AppUserDevice> TrustedDevices { get; } = [];

    [JsonIgnore]
    public virtual ICollection<Fido2Credential> Fido2Credentials { get; } = [];


    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();


    public static AppUser Create(
        Guid tenantId,
        Guid? employeeId,
        string userName,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string createdBy
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId.ToString());
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUser
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(), // Initialize ConcurrencyStamp for optimistic concurrency control
            SecurityStamp = Guid.CreateVersion7().ToString(), // Initialize other properties with default values if necessary
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // Factory for Employee-linked user
    public static AppUser CreateForEmployee(
        Guid tenantId,
        Guid employeeId,
        string userName,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string nationalId,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUser
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            CustomerId = null,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            NationalId = nationalId,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(),
            SecurityStamp = Guid.CreateVersion7().ToString(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // Factory for Customer-linked user
    public static AppUser CreateForCustomer(
        Guid tenantId,
        Guid customerId,
        string userName,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string nationalId,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUser
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = tenantId,
            EmployeeId = null,
            CustomerId = customerId,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            NationalId = nationalId,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(),
            SecurityStamp = Guid.CreateVersion7().ToString(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // Factory for system/admin users (no business relationship)
    public static AppUser CreateSystemUser(
        Guid tenantId,
        string userName,
        string email,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUser
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = tenantId,
            EmployeeId = null,
            CustomerId = null,
            UserName = userName,
            Email = email,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(),
            SecurityStamp = Guid.CreateVersion7().ToString(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // Factory for external SSO users provisioned just-in-time
    public static AppUser CreateExternalUser(
        Guid tenantId,
        string userName,
        string email,
        string firstName,
        string lastName,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUser
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = tenantId,
            EmployeeId = null,
            CustomerId = null,
            UserName = userName,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(),
            SecurityStamp = Guid.CreateVersion7().ToString(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            ActivatedAt = DateTimeOffset.UtcNow,
            ActivatedBy = createdBy,
            RequirePasswordChange = false
        };
    }

    // Linking behaviours — called when role changes post-creation
    public void LinkToEmployee(Guid employeeId)
    {
        if (EmployeeId.HasValue)
            throw new DomainException("User is already linked to an employee record.");

        EmployeeId = employeeId;
        RaiseDomainEvent(new EmployeeLinkedToUserEvent(Id, employeeId));
    }

    public void LinkToCustomer(Guid customerId)
    {
        if (CustomerId.HasValue)
            throw new DomainException("User is already linked to a customer record.");

        CustomerId = customerId;
        RaiseDomainEvent(new CustomerLinkedToUserEvent(Id, customerId));
    }

    public void RaiseAppUserCreatedEvent()
    {
        RaiseDomainEvent(new AppUserCreatedEvent(
            Id,
            TenantId,
            EmployeeId,
            UserName!,
            $"{FirstName} {LastName}",
            Email ?? string.Empty));
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LastFailedLoginAt = null;
    }

    public void SetIdentityProfile(string? nationalId, Gender gender, string? registrationNumber = null)
    {
        NationalId = nationalId?.Trim() ?? string.Empty;
        RegistrationNumber = registrationNumber?.Trim() ?? string.Empty;
        Gender = gender;
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        ResetFailedLoginAttempts();
    }

    public void CompletePasswordReset(string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        ResetFailedLoginAttempts();
        PasswordLastChanged = DateTimeOffset.UtcNow;
        RequirePasswordChange = false;
        MarkUpdated(updatedBy);
    }

    public void MarkUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetProfilePicture(Uri profilePictureUrl, string updatedBy)
    {
        ArgumentNullException.ThrowIfNull(profilePictureUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        ProfilePictureUrl = profilePictureUrl;
        MarkUpdated(updatedBy);
    }

    public void UpdateAdminProfile(
        string userName,
        string email,
        string firstName,
        string lastName,
        string? phoneNumber,
        string? nationalId,
        Gender gender,
        string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        UserName = userName.Trim();
        Email = email.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        NationalId = nationalId?.Trim() ?? string.Empty;
        Gender = gender;
        MarkUpdated(updatedBy);
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentNullException.ThrowIfNull(deletedBy);
        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletedAt = DateTimeOffset.UtcNow;

    }

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);

    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void GrantAccess(string grantedBy, IEnumerable<string> defaultRoles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(grantedBy);

        if (IsActive)
            throw new DomainException("User account is already active.");

        var now = DateTimeOffset.UtcNow;

        IsActive = true;
        ActivatedAt = now;
        ActivatedBy = grantedBy;
        UpdatedAt = now;
        UpdatedBy = grantedBy;
        DeactivatedAt = null;
        DeactivatedBy = null;
        DeactivationReason = null;
        RequirePasswordChange = true; // Force password set on first login

        RaiseDomainEvent(new AppUserAccessGrantedEvent(Id, EmployeeId, CustomerId, grantedBy));
    }

    public void RevokeAccess(string revokedBy, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (!IsActive)
            throw new DomainException("User account is already inactive.");

        var now = DateTimeOffset.UtcNow;

        IsActive = false;
        DeactivatedAt = now;
        DeactivatedBy = revokedBy;
        DeactivationReason = reason;
        UpdatedAt = now;
        UpdatedBy = revokedBy;

        RaiseDomainEvent(new AppUserAccessRevokedEvent(Id, EmployeeId, CustomerId, revokedBy, reason));
    }
}
