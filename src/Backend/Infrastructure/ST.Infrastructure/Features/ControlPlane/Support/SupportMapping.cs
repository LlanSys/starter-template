using ST.Domain.Features.ControlPlane.Support.Entities;
using ST.SharedKernel.Features.ControlPlane.Support;
namespace ST.Infrastructure.Features.ControlPlane.Support;
internal static class SupportMapping
{
 internal static SupportGrantResponse ToResponse(SupportGrant g)=>new(g.Id,g.WorkspaceId,g.VendorId,g.Reason,g.PermissionKeys,g.ReadOnly,g.DurationMinutes,g.Status,g.ApprovedBy,g.ExpiresAt);
}
