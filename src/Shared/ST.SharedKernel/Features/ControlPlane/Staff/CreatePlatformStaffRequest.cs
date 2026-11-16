namespace ST.SharedKernel.Features.ControlPlane.Staff;

public sealed record CreatePlatformStaffRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string[] PermissionKeys,
    string Reason
);
