using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;

namespace ST.Application.Features.IAM.Permissions.Commands;



internal sealed class CreatePermissionCommandHandler(IIamUnitOfWork unitOfWork, ILogger<CreatePermissionCommandHandler> logger, IEffectiveModulePolicy policy)
    : IRequestHandler<CreatePermissionCommand, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(CreatePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            if (!access.Modules.Contains(command.Request.ModuleKey)) return AppResponses.Failure<PermissionResponse>("Select an enabled module for this permission.");
            var requestedKey = command.Request.Resource.Trim().ToLowerInvariant().Replace(' ', '_') + "." + command.Request.Action.Trim().ToLowerInvariant().Replace(' ', '_');
            if (requestedKey.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) || (EffectiveModuleAccess.TenantSettingsPermissions.Contains(requestedKey, StringComparer.OrdinalIgnoreCase)
                || access.Catalogue.Any(entry => entry.Kind == ModuleCatalogueKind.Permission && string.Equals(entry.Key, requestedKey, StringComparison.OrdinalIgnoreCase)))) return AppResponses.Failure<PermissionResponse>("Standard permissions are managed in the platform module catalogue.");
            var catalogError = await ValidateCatalogAsync(unitOfWork, command.Request.Context, command.Request.Resource, command.Request.Action, cancellationToken, access)
                .ConfigureAwait(false);
            if (catalogError is not null)
            {
                return AppResponses.Failure<PermissionResponse>(catalogError);
            }

            var permission = Permission.Create(

                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.UserId);

            permission.SetModule(command.Request.ModuleKey);
            var duplicate = await unitOfWork.PermissionRepository
                .HasUsedKeyAsync(permission.Key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<PermissionResponse>($"Permission {permission.Key} already exists.");
            }

            await unitOfWork.PermissionRepository.CreateAsync(permission, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Permission created.", permission.ToPermissionResponse())
                : AppResponses.Failure<PermissionResponse>("Permission create failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(CreatePermissionCommandHandler), ex);
            throw;
        }
    }

    private static async Task<string?> ValidateCatalogAsync(
        IIamUnitOfWork unitOfWork,
        string context,
        string resource,
        string action,
        CancellationToken cancellationToken, EffectiveModuleAccess? access = null)
    {
        var standards = access?.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Permission && access.EntryAvailable(x)).Select(ModuleCatalogueMapping.ToPermissionResponse).ToArray() ?? [];
        var contextExists = await unitOfWork.PermissionContextRepository
            .AnyAsync(item => item.IsActive && item.Key == context.Trim(), cancellationToken)
            .ConfigureAwait(false);

        contextExists |= standards.Any(x => x.Context.Equals(context, StringComparison.OrdinalIgnoreCase));
        if (!contextExists)
        {
            return $"Permission context '{context}' is not registered.";
        }

        var resourceExists = await unitOfWork.PermissionResourceRepository
            .AnyAsync(item => item.IsActive && item.ContextKey == context.Trim() && item.Key == resource.Trim().ToLowerInvariant().Replace(' ', '_'), cancellationToken)
            .ConfigureAwait(false);

        resourceExists |= standards.Any(x => x.Context.Equals(context, StringComparison.OrdinalIgnoreCase) && x.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase));
        if (!resourceExists)
        {
            return $"Permission resource '{resource}' is not registered for context '{context}'.";
        }

        var actionExists = await unitOfWork.PermissionActionRepository
            .AnyAsync(item => item.IsActive && item.Key == action.Trim().ToLowerInvariant().Replace(' ', '_'), cancellationToken)
            .ConfigureAwait(false);

        actionExists |= standards.Any(x => x.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        return actionExists ? null : $"Permission action '{action}' is not registered.";
    }
}
