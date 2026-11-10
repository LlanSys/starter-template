using MediatR;
using Microsoft.AspNetCore.Http;
using ST.Application.Features.ControlPlane.Support;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Support;
namespace ST.Infrastructure.Features.ControlPlane.Support;
internal sealed class GetSupportGrantsHandler(IControlPlaneUnitOfWork plane,ICurrentTenantProvider tenant,IHttpContextAccessor http) : IRequestHandler<GetSupportGrantsQuery,AppResponse<IReadOnlyList<SupportGrantResponse>>>
{
 public async Task<AppResponse<IReadOnlyList<SupportGrantResponse>>> Handle(GetSupportGrantsQuery request,CancellationToken ct) {
 if(request.Platform && !PlatformAccess.IsStaff(http.HttpContext!.User)) return AppResponses.Failure<IReadOnlyList<SupportGrantResponse>>("Platform access is required.");
 var id=tenant.TenantId;var rows=await plane.SupportGrants.ListAsync(q=>q.Where(x=>request.Platform || x.WorkspaceId==id).OrderByDescending(x=>x.CreatedAt).Take(200),ct).ConfigureAwait(false);
 return AppResponses.Success<IReadOnlyList<SupportGrantResponse>>(rows.Select(SupportMapping.ToResponse).ToArray());
 }
}
