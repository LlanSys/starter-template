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



internal sealed class CreatePermissionCommandHandler(IIamUnitOfWork unitOfWork, ILogger<CreatePermissionCommandHandler> logger)
    : IRequestHandler<CreatePermissionCommand, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(CreatePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var catalogError = await ValidateCatalogAsync(unitOfWork, command.Request.Context, command.Request.Resource, command.Request.Action, cancellationToken)
                .ConfigureAwait(false);
            if (catalogError is not null)
            {
                return AppResponses.Failure<PermissionResponse>(catalogError);
            }

            var permission = Permission.Create(
                command.Request.DepartmentId,
                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.UserId);

            var duplicate = await unitOfWork.PermissionRepository
                .AnyAsync(existing => existing.Key == permission.Key, cancellationToken)
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
        CancellationToken cancellationToken)
    {
        var contextExists = await unitOfWork.PermissionContextRepository
            .AnyAsync(item => item.IsActive && item.Key == context.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (!contextExists)
        {
            return $"Permission context '{context}' is not registered.";
        }

        var resourceExists = await unitOfWork.PermissionResourceRepository
            .AnyAsync(item => item.IsActive && item.ContextKey == context.Trim() && item.Key == resource.Trim().ToLowerInvariant().Replace(' ', '_'), cancellationToken)
            .ConfigureAwait(false);

        if (!resourceExists)
        {
            return $"Permission resource '{resource}' is not registered for context '{context}'.";
        }

        var actionExists = await unitOfWork.PermissionActionRepository
            .AnyAsync(item => item.IsActive && item.Key == action.Trim().ToLowerInvariant().Replace(' ', '_'), cancellationToken)
            .ConfigureAwait(false);

        return actionExists ? null : $"Permission action '{action}' is not registered.";
    }
}
