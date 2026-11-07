using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Permissions.Commands;



internal sealed class UpdatePermissionCommandHandler(IIamUnitOfWork unitOfWork, ILogger<UpdatePermissionCommandHandler> logger, ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy policy)
    : IRequestHandler<UpdatePermissionCommand, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            if (!access.Modules.Contains(command.Request.ModuleKey)) return AppResponses.Failure<PermissionResponse>("Select an enabled module for this permission.");
            var requestedKey = command.Request.Resource.Trim().ToLowerInvariant().Replace(' ', '_') + "." + command.Request.Action.Trim().ToLowerInvariant().Replace(' ', '_');
            if (requestedKey.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) || access.Catalogue.Any(x => x.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Permission && x.Key == requestedKey)) return AppResponses.Failure<PermissionResponse>("Standard permissions are managed in the platform module catalogue.");
            var catalogError = await ValidateCatalogAsync(unitOfWork, command.Request.Context, command.Request.Resource, command.Request.Action, cancellationToken, access)
                .ConfigureAwait(false);
            if (catalogError is not null)
            {
                return AppResponses.Failure<PermissionResponse>(catalogError);
            }

            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (permission is null)
            {
                return AppResponses.Failure<PermissionResponse>($"Permission {command.Id} not found.");
            }

            var draft = Permission.Create(

                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.UserId);

            var duplicate = await unitOfWork.PermissionRepository
                .AnyAsync(existing => existing.Id != command.Id && existing.Key == draft.Key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<PermissionResponse>($"Permission {draft.Key} already exists.");
            }

            permission.SetModule(command.Request.ModuleKey);
            permission.Update(

                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.Request.IsActive,
                command.UserId);

            await unitOfWork.PermissionRepository.UpdateAsync(permission, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Permission updated.", permission.ToPermissionResponse())
                : AppResponses.Failure<PermissionResponse>("Permission update failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(UpdatePermissionCommandHandler), ex);
            throw;
        }
    }

    private static async Task<string?> ValidateCatalogAsync(
        IIamUnitOfWork unitOfWork,
        string context,
        string resource,
        string action,
        CancellationToken cancellationToken, ST.Application.Features.ControlPlane.Modules.EffectiveModuleAccess? access = null)
    {
        var standards = access?.Catalogue.Where(x => x.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Permission && access.EntryAvailable(x)).Select(ST.Application.Features.ControlPlane.Modules.ModuleCatalogueMapping.ToPermissionResponse).ToArray() ?? [];
        var normalizedResource = resource.Trim().ToLowerInvariant().Replace(' ', '_');
        var normalizedAction = action.Trim().ToLowerInvariant().Replace(' ', '_');
        var normalizedContext = context.Trim();

        var contextExists = await unitOfWork.PermissionContextRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedContext, cancellationToken)
            .ConfigureAwait(false);

        contextExists |= standards.Any(x => x.Context.Equals(context, StringComparison.OrdinalIgnoreCase));
        if (!contextExists)
        {
            return $"Permission context '{context}' is not registered.";
        }

        var resourceExists = await unitOfWork.PermissionResourceRepository
            .AnyAsync(item => item.IsActive && item.ContextKey == normalizedContext && item.Key == normalizedResource, cancellationToken)
            .ConfigureAwait(false);

        resourceExists |= standards.Any(x => x.Context.Equals(context, StringComparison.OrdinalIgnoreCase) && x.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase));
        if (!resourceExists)
        {
            return $"Permission resource '{resource}' is not registered for context '{context}'.";
        }

        var actionExists = await unitOfWork.PermissionActionRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedAction, cancellationToken)
            .ConfigureAwait(false);

        actionExists |= standards.Any(x => x.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        return actionExists ? null : $"Permission action '{action}' is not registered.";
    }
}
