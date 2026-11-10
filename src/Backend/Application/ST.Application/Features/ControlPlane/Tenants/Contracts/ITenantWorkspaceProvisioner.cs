using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Application.Features.ControlPlane.Tenants.Contracts;
public interface ITenantWorkspaceProvisioner
{
    Task<AppResponse<WorkspaceSetupResponse>> InitializeAsync(Guid tenantId, InitializeWorkspaceRequest? administrator, string actorId, CancellationToken cancellationToken);
    Task<AppResponse<WorkspaceSetupResponse>> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken);
}
