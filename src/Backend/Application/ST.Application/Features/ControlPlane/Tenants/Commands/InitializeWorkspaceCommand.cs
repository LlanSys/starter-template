using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Application.Features.ControlPlane.Tenants.Commands;
public sealed record InitializeWorkspaceCommand(Guid TenantId, InitializeWorkspaceRequest Request, string ActorId) : IRequest<AppResponse<WorkspaceSetupResponse>>;
