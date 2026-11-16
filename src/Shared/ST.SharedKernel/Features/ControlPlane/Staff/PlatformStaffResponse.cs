namespace ST.SharedKernel.Features.ControlPlane.Staff;

public sealed record PlatformStaffResponse(
    string UserId,
    string Username,
    string FullName,
    string Email,
    bool IsStaff,
    bool IsActive,
    bool EmailConfirmed,
    bool MfaEnabled,
    string Status,
    string Revision,
    IReadOnlyList<string> PermissionKeys
);
