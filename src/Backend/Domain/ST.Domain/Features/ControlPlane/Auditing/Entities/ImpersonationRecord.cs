using ST.Domain.Features.ControlPlane.Auditing.Enums;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ST.Domain.Features.ControlPlane.Auditing.Entities;

public class ImpersonationRecord : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string ActorId { get; set; }
    public required string ActorName { get; set; }
    public required Guid TargetTenantId { get; set; }
    public required string TargetTenantName { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset ExpiryTime { get; set; }

    // Status can be updated if the user explicitly exits early.
    // Otherwise, we rely on lazy evaluation based on ExpiryTime.
    public ImpersonationRecordStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];
}
