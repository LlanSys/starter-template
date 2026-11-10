using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
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

    public Guid? AdministratorInvitationId { get; private set; }
    public DateTimeOffset? AdministratorSetupCompletedAt { get; private set; }
    public bool RequiresAdministratorSetup => AdministratorInvitationId.HasValue && !AdministratorSetupCompletedAt.HasValue;

    public void BeginAdministratorSetup(Guid invitationId)
    {
        AdministratorInvitationId = invitationId;
        AdministratorSetupCompletedAt = null;
    }

    public void CompleteAdministratorSetup(DateTimeOffset now)
    {
        AdministratorSetupCompletedAt = now;
    }




    // Minimal personal data needed for auth UX only


    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;


    public Uri? ProfilePictureUrl { get; set; }


    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LastFailedLoginAt { get; set; }
    public bool RequirePasswordChange { get; set; }
    public DateTimeOffset? PasswordLastChanged { get; set; }

    public string? TotpSecret { get; set; }

    public bool IsActive { get; set; }


    public DateTimeOffset CreatedAt { get; set; }
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


    public static AppUser CreateAccount(
        DateTimeOffset now,
        Guid tenantId, string userName, string firstName, string lastName,
        string email, string? phoneNumber, string createdBy)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("An account requires a tenant identity.", nameof(tenantId));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var user = CreateSystemUser(now, tenantId, userName.Trim(), email.Trim(), createdBy);
        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.PhoneNumber = phoneNumber?.Trim();
        return user;
    }




    // Factory for system/admin users (no business relationship)
    public static AppUser CreateSystemUser(
        DateTimeOffset now,
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


            UserName = userName,
            Email = email,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(),
            SecurityStamp = Guid.CreateVersion7().ToString(),
            CreatedBy = createdBy,
            CreatedAt = now,
        };
    }

    // Factory for external SSO users provisioned just-in-time
    public static AppUser CreateExternalUser(
        DateTimeOffset now,
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


            UserName = userName,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            ConcurrencyStamp = Guid.CreateVersion7().ToString(),
            SecurityStamp = Guid.CreateVersion7().ToString(),
            CreatedBy = createdBy,
            CreatedAt = now,
            IsActive = true,
            ActivatedAt = now,
            ActivatedBy = createdBy,
            RequirePasswordChange = false
        };
    }





    public void RaiseAppUserCreatedEvent(DateTimeOffset now)
    {
        RaiseDomainEvent(new AppUserCreatedEvent(
            Id,
            TenantId,
            UserName!,
            $"{FirstName} {LastName}",
            Email ?? string.Empty, now));
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LastFailedLoginAt = null;
    }



    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        ResetFailedLoginAttempts();
    }

    public void CompletePasswordReset(DateTimeOffset now, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        ResetFailedLoginAttempts();
        PasswordLastChanged = now;
        RequirePasswordChange = false;
        MarkUpdated(now, updatedBy);
    }

    public void MarkUpdated(DateTimeOffset now, string? updatedBy = null)
    {
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetProfilePicture(DateTimeOffset now, Uri profilePictureUrl, string updatedBy)
    {
        ArgumentNullException.ThrowIfNull(profilePictureUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        ProfilePictureUrl = profilePictureUrl;
        MarkUpdated(now, updatedBy);
    }

    public void UpdateAdminProfile(
        DateTimeOffset now,
        string userName,
        string email,
        string firstName,
        string lastName,
        string? phoneNumber,
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
        MarkUpdated(now, updatedBy);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(deletedBy);
        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletedAt = now;

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

    public void GrantAccess(DateTimeOffset now, string grantedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(grantedBy);

        if (IsActive)
            throw new DomainException("User account is already active.");


        IsActive = true;
        ActivatedAt = now;
        ActivatedBy = grantedBy;
        UpdatedAt = now;
        UpdatedBy = grantedBy;
        DeactivatedAt = null;
        DeactivatedBy = null;
        DeactivationReason = null;
        RequirePasswordChange = true; // Force password set on first login

        RaiseDomainEvent(new AppUserAccessGrantedEvent(Id, TenantId, grantedBy, now));
    }

    public void RevokeAccess(DateTimeOffset now, string revokedBy, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (!IsActive)
            throw new DomainException("User account is already inactive.");


        IsActive = false;
        DeactivatedAt = now;
        DeactivatedBy = revokedBy;
        DeactivationReason = reason;
        UpdatedAt = now;
        UpdatedBy = revokedBy;

        RaiseDomainEvent(new AppUserAccessRevokedEvent(Id, TenantId, revokedBy, reason, now));
    }
}
