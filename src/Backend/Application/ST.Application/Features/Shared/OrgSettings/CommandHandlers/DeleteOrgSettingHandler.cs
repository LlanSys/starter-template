using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.Shared.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using System.Net;

namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;

internal sealed class DeleteOrgSettingHandler(ISharedUnitOfWork unitOfWork)
    : IRequestHandler<DeleteOrgSettingCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteOrgSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.OrgSettingRepository.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (setting is null)
            return AppResponses.Failure<bool>("Tenant setting not found.");

        await unitOfWork.OrgSettingRepository.DeleteAsync(setting.Id, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success("Tenant setting deleted successfully.", true);
    }
}
