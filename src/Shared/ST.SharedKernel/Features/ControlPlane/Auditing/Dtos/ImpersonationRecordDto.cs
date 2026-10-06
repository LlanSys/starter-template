using System;

namespace ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;

public record ImpersonationRecordDto(
    Guid Id,
    string ActorId,
    string ActorName,
    Guid TargetTenantId,
    string TargetTenantName,
    string Reason,
    DateTimeOffset StartTime,
    DateTimeOffset ExpiryTime,
    string Status);
