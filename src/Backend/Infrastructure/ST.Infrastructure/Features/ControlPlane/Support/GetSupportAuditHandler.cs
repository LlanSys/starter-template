using MediatR;
using Microsoft.AspNetCore.Http;
using ST.Application.Features.ControlPlane.Support;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Support;
namespace ST.Infrastructure.Features.ControlPlane.Support;
internal sealed class GetSupportAuditHandler(IControlPlaneUnitOfWork plane,ICurrentTenantProvider tenant,IHttpContextAccessor http) : IRequestHandler<GetSupportAuditQuery,AppResponse<IReadOnlyList<SupportAuditResponse>>>
{
 public async Task<AppResponse<IReadOnlyList<SupportAuditResponse>>> Handle(GetSupportAuditQuery request,CancellationToken ct) {
 var grant=await plane.SupportGrants.FindByIdAsync(request.GrantId,ct).ConfigureAwait(false);
 if(grant is null || (request.Platform ? !PlatformAccess.IsStaff(http.HttpContext!.User) : grant.WorkspaceId!=tenant.TenantId)) return AppResponses.NotFound<IReadOnlyList<SupportAuditResponse>>("Support request not found.");
 var rows=await plane.SupportAudit.ListAsync(q=>q.Where(x=>x.GrantId==grant.Id).OrderByDescending(x=>x.CreatedAt).Take(500).Select(x=>new SupportAuditResponse(x.CreatedAt,x.ActorId,x.Action,x.StatusCode)),ct).ConfigureAwait(false);
 return AppResponses.Success<IReadOnlyList<SupportAuditResponse>>(rows);
 }
}
