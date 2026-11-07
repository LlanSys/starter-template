using MediatR;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;
internal sealed class InitializeWorkspaceHandler(ITenantWorkspaceProvisioner provisioner) : IRequestHandler<InitializeWorkspaceCommand, AppResponse<WorkspaceSetupResponse>>
{
 public Task<AppResponse<WorkspaceSetupResponse>> Handle(InitializeWorkspaceCommand request, CancellationToken ct) => provisioner.InitializeAsync(request.TenantId, request.Request, request.ActorId, ct);
}
