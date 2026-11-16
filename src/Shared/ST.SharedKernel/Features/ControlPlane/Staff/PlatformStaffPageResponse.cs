namespace ST.SharedKernel.Features.ControlPlane.Staff;

public sealed record PlatformStaffPageResponse(
    IReadOnlyList<PlatformStaffResponse> Items,
    int Total,
    int Page,
    int PageSize
);
