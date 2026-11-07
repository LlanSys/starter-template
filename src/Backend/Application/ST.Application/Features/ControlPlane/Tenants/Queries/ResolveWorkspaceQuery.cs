using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.Application.Contracts.Interfaces.Common;
namespace ST.Application.Features.ControlPlane.Tenants.Queries;
public sealed record ResolveWorkspaceQuery(string? Identifier, Guid? TenantId) : IRequest<AppResponse<WorkspaceInfoResponse>>, ICachableRequest
{
    public string CacheGroup => "public-workspace-routing";
    public string Discriminator => Identifier ?? TenantId?.ToString() ?? "default";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
