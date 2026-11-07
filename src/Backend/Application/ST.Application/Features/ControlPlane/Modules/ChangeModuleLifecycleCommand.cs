using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed record ChangeModuleLifecycleCommand(Guid Id, ModuleLifecycleRequest Request, string ActorId) : IRequest<AppResponse<ModuleResponse>>;
