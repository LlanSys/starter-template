using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Application.Features.ControlPlane.Staff;

public interface IPlatformStaffService
{
    Task<AppResponse<PlatformStaffPageResponse>> ListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken
    );
    Task<AppResponse<IReadOnlyList<PlatformStaffAuditResponse>>> HistoryAsync(
        string userId,
        CancellationToken cancellationToken
    );
    Task<AppResponse<PlatformStaffResponse>> CreateAsync(
        CreatePlatformStaffRequest request,
        CancellationToken cancellationToken
    );
    Task<AppResponse<PlatformStaffResponse>> UpdateAsync(
        string userId,
        UpdatePlatformStaffRequest request,
        CancellationToken cancellationToken
    );
    Task<AppResponse<bool>> InviteAsync(
        string userId,
        PlatformStaffInvitationRequest request,
        CancellationToken cancellationToken
    );
}
