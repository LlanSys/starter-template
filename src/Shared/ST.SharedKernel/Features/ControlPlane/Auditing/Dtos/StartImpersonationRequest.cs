using System;

namespace ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;

public record StartImpersonationRequest(
    Guid TargetTenantId,
    string Reason,
    int DurationHours = 1);
