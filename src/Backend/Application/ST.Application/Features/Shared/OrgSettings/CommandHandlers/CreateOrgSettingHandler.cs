using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.Shared.Contracts;
using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.Application.Features.Shared.OrgSettings.Mappings;
using MediatR;
using System.Net;

namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;

internal sealed class CreateOrgSettingHandler(
    ISharedUnitOfWork unitOfWork,
    IEncryptionService encryptionService)
    : IRequestHandler<CreateOrgSettingCommand, AppResponse<OrgSettingResponse>>
{
    public async Task<AppResponse<OrgSettingResponse>> Handle(CreateOrgSettingCommand request, CancellationToken cancellationToken)
    {
        var existingSetting = await unitOfWork.OrgSettingRepository.FirstOrDefaultAsync(x => x.Key == request.Request.Key, cancellationToken).ConfigureAwait(false);

        if (existingSetting != null)
        {
            return AppResponses.Failure<OrgSettingResponse>($"A setting with key '{request.Request.Key}' already exists.");
        }

        var setting = new OrgSetting(
            request.Request.Key,
            OrgSettingMapping.IsSensitiveKey(request.Request.Key)
                ? encryptionService.Encrypt(request.Request.Value)
                : request.Request.Value,
            request.UserId,
            request.Request.Description);

        await unitOfWork.OrgSettingRepository.CreateAsync(setting, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var response = setting.ToResponse();
        return AppResponses.Success("Tenant setting created successfully.", response);
    }
}
