using ST.Domain.Shared.Contracts.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using ST.Domain.Features.ControlPlane.Tenants.Enums;

namespace ST.Domain.Features.ControlPlane.Tenants.Entities;

public class Tenant : IAuditable, IHasDomainEvents
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Identifier { get; set; } // URL-safe slug
    public required string DisplayName { get; set; }
    public required string HostName { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsDemo { get; set; }
    public int DemoFixtureVersion { get; set; }
    public DateTimeOffset? DemoResetAt { get; set; }
    public int WorkspaceVersion { get; set; }
    public DateTimeOffset? WorkspaceInitializedAt { get; set; }
    public string? AdministratorUserId { get; set; }
    public string? AdministratorEmail { get; set; }
    public DateTimeOffset? InvitationSentAt { get; set; }
    public int MaxUsers { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.PendingApproval;

    public static readonly Guid PlatformTenantId = Guid.Parse("0194f700-0000-7000-8000-000000000001");

    public DateTimeOffset? KycApprovedAt { get; private set; }
    public string? KycApprovedBy { get; private set; }
    public string? KycEvidenceReference { get; private set; }

    public bool IsCustomerWorkspace => !IsDemo && Id != PlatformTenantId;
    public bool CanProvision => !IsCustomerWorkspace || KycApprovedAt.HasValue;
    public bool CanInviteAdministrator => IsCustomerWorkspace && KycApprovedAt.HasValue && Status == TenantStatus.Active;

    public void Approve(string actorId, DateTimeOffset now, string? evidenceReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (IsCustomerWorkspace)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
            if (evidenceReference.Trim().Length > 256)
            {
                throw new ArgumentException("The evidence reference must be at most 256 characters.", nameof(evidenceReference));
            }

            if (KycApprovedAt.HasValue)
            {
                throw new InvalidOperationException("KYC is already approved.");
            }

            KycApprovedAt = now;
            KycApprovedBy = actorId;
            KycEvidenceReference = evidenceReference.Trim();
        }
        else if (Status != TenantStatus.PendingApproval)
        {
            throw new InvalidOperationException("The platform workspace is already initialized.");
        }

        if (Status == TenantStatus.PendingApproval)
        {
            Status = TenantStatus.PendingProvisioning;
        }

        UpdatedBy = actorId;
        UpdatedAt = now;
    }

    public void RetryProvisioning()
    {
        if (!CanProvision || Status != TenantStatus.ProvisioningFailed)
        {
            throw new InvalidOperationException("Only approved workspaces with failed provisioning can retry.");
        }

        Status = TenantStatus.PendingProvisioning;
    }

    public void MarkAsProvisioning()
    {
        if (!CanProvision || Status != TenantStatus.PendingProvisioning)
            throw new InvalidOperationException($"Cannot start provisioning from status {Status}");

        Status = TenantStatus.Provisioning;
    }

    public void MarkAsActive()
    {
        if (!CanProvision || Status is not (TenantStatus.PendingProvisioning or TenantStatus.Provisioning or TenantStatus.Suspended))
            throw new InvalidOperationException($"Cannot activate tenant from status {Status}");

        Status = TenantStatus.Active;
    }

    public void Suspend()
    {
        if (Status != TenantStatus.Active)
            throw new InvalidOperationException($"Cannot suspend tenant from status {Status}");

        Status = TenantStatus.Suspended;
    }

    public void MarkAsProvisioningFailed()
    {
        Status = TenantStatus.ProvisioningFailed;
    }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public Guid DeploymentStampId { get; set; }
    public DeploymentStamp? DeploymentStamp { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }

    public ICollection<TenantModule> Modules { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];
}
