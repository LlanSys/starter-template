using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Permissions.Commands;



internal sealed class DeletePermissionCommandHandler(IIamUnitOfWork unitOfWork, ILogger<DeletePermissionCommandHandler> logger)
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

            permission.MarkAsDeleted(command.UserId);
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
