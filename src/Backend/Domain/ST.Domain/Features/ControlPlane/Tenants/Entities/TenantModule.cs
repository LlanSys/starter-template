using ST.Domain.Shared.Contracts.Common;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ST.Domain.Features.ControlPlane.Tenants.Entities;

public class TenantModule : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string ModuleKey { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];
}
