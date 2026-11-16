namespace ST.SharedKernel.Features.ControlPlane.Staff;

public sealed record UpdatePlatformStaffRequest(
    string Revision,
    bool IsStaff,
    bool IsActive,
    string[] PermissionKeys,
    string Reason
);
