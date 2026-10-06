using ST.Domain.Shared.Contracts.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using ST.Domain.Features.ControlPlane.Tenants.Enums;

namespace ST.Domain.Features.ControlPlane.Tenants.Entities;

public class DeploymentStamp : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string TargetResourceGroup { get; set; }
    public IsolationTier IsolationTier { get; set; }

    public string? KeyVaultUri { get; set; }
    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];
}
