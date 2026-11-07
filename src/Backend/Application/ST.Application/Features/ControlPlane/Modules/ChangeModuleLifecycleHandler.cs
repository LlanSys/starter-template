using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class ChangeModuleLifecycleHandler(IControlPlaneUnitOfWork unit, TimeProvider timeProvider) : IRequestHandler<ChangeModuleLifecycleCommand, AppResponse<ModuleResponse>>
{
    public async Task<AppResponse<ModuleResponse>> Handle(ChangeModuleLifecycleCommand command, CancellationToken cancellationToken)
    {
        var module = await unit.Modules.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (module is null) return AppResponses.Failure<ModuleResponse>("Module not found.");
        if (!module.ChangeAvailability(timeProvider.GetUtcNow(), command.Request.Action == ModuleLifecycleAction.Enable, command.Request.Action == ModuleLifecycleAction.Retire, command.ActorId))
            return AppResponses.Failure<ModuleResponse>(module.IsEssential ? "Core workspace and identity capabilities must remain active." : "Retired modules cannot be changed.");
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(new ModuleResponse(module.Id, module.Key, module.Name, module.Description, module.IsActive, ModuleImplementations.Contains(module.Key)) { IsRetired = module.IsRetired });
    }
}
