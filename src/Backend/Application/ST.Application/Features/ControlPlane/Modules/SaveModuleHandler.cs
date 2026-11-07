using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class SaveModuleHandler(IControlPlaneUnitOfWork unitOfWork) : IRequestHandler<SaveModuleCommand, AppResponse<ModuleResponse>>
{
    public async Task<AppResponse<ModuleResponse>> Handle(SaveModuleCommand command, CancellationToken cancellationToken)
    {
        var key = command.Request.Key.Trim();
        if (!command.Request.IsActive && (key.Equals("Core", StringComparison.OrdinalIgnoreCase) || key.Equals("IAM", StringComparison.OrdinalIgnoreCase)))
            return AppResponses.Failure<ModuleResponse>("Core workspace and identity capabilities must remain active.");
        var known = await unitOfWork.Modules.ListAsync(query => query.Select(module => new { module.Id, module.Key }), cancellationToken).ConfigureAwait(false);
        var duplicate = known.Any(module => module.Id != command.Id && string.Equals(module.Key, key, StringComparison.OrdinalIgnoreCase));
        if (duplicate) return AppResponses.Failure<ModuleResponse>("A module with this key already exists.");
        var module = command.Id.HasValue ? await unitOfWork.Modules.FindByIdAsync(command.Id.Value, cancellationToken).ConfigureAwait(false) : null;
        if (command.Id.HasValue && module is null) return AppResponses.Failure<ModuleResponse>("Module not found.");
        if (module?.IsRetired == true) return AppResponses.Failure<ModuleResponse>("Retired modules cannot be edited or re-enabled.");
        if (module is not null && !string.Equals(module.Key, key, StringComparison.Ordinal)) return AppResponses.Failure<ModuleResponse>("A module key cannot change after registration.");
        if (module is null)
        {
            module = new ModuleDefinition { Key = key.ToLowerInvariant(), Name = command.Request.Name.Trim(), CreatedBy = command.ActorId };
            await unitOfWork.Modules.CreateAsync(module, cancellationToken).ConfigureAwait(false);
        }
        if (command.Request.DependencyKeys is { } requestedDependencies)
        {
            var dependencies = requestedDependencies.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var definitions = await unitOfWork.Modules.ListAsync(ct: cancellationToken).ConfigureAwait(false);
            if (dependencies.Any(dependency => !definitions.Any(x => string.Equals(x.Key, dependency, StringComparison.OrdinalIgnoreCase)))) return AppResponses.Failure<ModuleResponse>("Select registered module dependencies.");
            var graph = definitions.ToDictionary(x => x.Key, x => x.DependencyKeys, StringComparer.OrdinalIgnoreCase);
            graph[module.Key] = dependencies;
            if (!ModuleDependencyGraph.IsAcyclic(graph)) return AppResponses.Failure<ModuleResponse>("Module dependencies cannot contain cycles.");
            module.DependencyKeys = dependencies;
        }
        module.Name = command.Request.Name.Trim(); module.Description = command.Request.Description.Trim(); module.IsActive = command.Request.IsActive;
        module.UpdatedAt = DateTimeOffset.UtcNow; module.UpdatedBy = command.ActorId;
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(new ModuleResponse(module.Id, module.Key, module.Name, module.Description, module.IsActive, ModuleImplementations.Contains(module.Key)));
    }
}
