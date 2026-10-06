using System;

namespace ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;

public record EndImpersonationRequest(Guid ImpersonationRecordId);
