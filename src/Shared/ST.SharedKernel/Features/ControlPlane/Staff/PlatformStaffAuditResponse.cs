namespace ST.SharedKernel.Features.ControlPlane.Staff;

public sealed record PlatformStaffAuditResponse(
    Guid Id,
    string UserId,
    string ActorId,
    string Action,
    string Reason,
    bool PreviousStaff,
    bool CurrentStaff,
    bool PreviousActive,
    bool CurrentActive,
    IReadOnlyList<string> PreviousPermissions,
    IReadOnlyList<string> CurrentPermissions,
    DateTimeOffset OccurredAt
);
