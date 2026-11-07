using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;

namespace ST.Application.Features.IAM.Permissions.Commands;



internal sealed class DeletePermissionCommandHandler(IIamUnitOfWork unitOfWork, ILogger<DeletePermissionCommandHandler> logger, IEffectiveModulePolicy policy, TimeProvider timeProvider)
    : IRequestHandler<DeletePermissionCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeletePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (permission is null)
            {
                return AppResponses.Failure<bool>($"Permission {command.Id} not found.");
            }

            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            if (access.Catalogue.Any(x => x.Key == permission.Key && x.Kind == ModuleCatalogueKind.Permission)) return AppResponses.Failure<bool>("Standard permissions are managed in the platform module catalogue.");
            permission.MarkAsDeleted(command.UserId, timeProvider.GetUtcNow());
            await unitOfWork.PermissionRepository.UpdateAsync(permission, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Permission deleted.", true)
                : AppResponses.Failure<bool>("Permission delete failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(DeletePermissionCommandHandler), ex);
            throw;
        }
    }
}
