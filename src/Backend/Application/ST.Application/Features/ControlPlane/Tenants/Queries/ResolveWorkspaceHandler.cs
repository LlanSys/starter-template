using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Application.Features.ControlPlane.Tenants.Queries;
internal sealed class ResolveWorkspaceHandler(IControlPlaneUnitOfWork unit, ICurrentTenantProvider current) : IRequestHandler<ResolveWorkspaceQuery, AppResponse<WorkspaceInfoResponse>>
{
    public async Task<AppResponse<WorkspaceInfoResponse>> Handle(ResolveWorkspaceQuery request, CancellationToken cancellationToken)
    {
        if (request.Identifier?.Length > 128) return AppResponses.Failure<WorkspaceInfoResponse>("Workspace is not available.");
        var identifier = request.Identifier?.Trim().ToLowerInvariant();
        var id = request.TenantId ?? (string.IsNullOrWhiteSpace(identifier) ? current.TenantId : Guid.Empty);
        var tenant = !string.IsNullOrWhiteSpace(identifier)
            ? await unit.Tenants.FirstOrDefaultAsync(x => x.Identifier == identifier && x.Status == TenantStatus.Active && !x.IsDemo, cancellationToken).ConfigureAwait(false)
            : await unit.Tenants.FirstOrDefaultAsync(x => x.Id == id && x.Status == TenantStatus.Active && !x.IsDemo, cancellationToken).ConfigureAwait(false);
        return tenant is null ? AppResponses.NotFound<WorkspaceInfoResponse>("Workspace is not available.") : AppResponses.Success(new WorkspaceInfoResponse(tenant.Id, tenant.Identifier, tenant.DisplayName, tenant.ClientOrigin));
    }
}
