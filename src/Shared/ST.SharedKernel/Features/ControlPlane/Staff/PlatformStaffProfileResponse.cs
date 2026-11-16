namespace ST.SharedKernel.Features.ControlPlane.Staff;

public sealed record PlatformStaffProfileResponse(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<string> PermissionKeys
);
