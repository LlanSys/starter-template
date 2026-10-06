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
    public int MaxUsers { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.PendingApproval;

    public void Approve(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (Status != TenantStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve tenant from status {Status}");

        Status = TenantStatus.PendingProvisioning;
        UpdatedBy = actorId;
        UpdatedAt = DateTimeOffset.UtcNow; // We will use TimeProvider in application layer, but here standard fallback
    }

    public void MarkAsProvisioning()
    {
        if (Status != TenantStatus.PendingProvisioning)
            throw new InvalidOperationException($"Cannot start provisioning from status {Status}");

        Status = TenantStatus.Provisioning;
    }

    public void MarkAsActive()
    {
        if (Status is not (TenantStatus.PendingProvisioning or TenantStatus.Provisioning or TenantStatus.Suspended))
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
    private readonly System.Collections.Generic.List<IDomainEvent> _domainEvents = [];
    public System.Collections.Generic.IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

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

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];
}
