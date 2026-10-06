using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.Shared.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.Application.Features.Shared.OrgSettings.Mappings;
using MediatR;
using System.Net;

namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;

internal sealed class UpdateOrgSettingHandler(
    ISharedUnitOfWork unitOfWork,
    IEncryptionService encryptionService)
    : IRequestHandler<UpdateOrgSettingCommand, AppResponse<OrgSettingResponse>>
{
    public async Task<AppResponse<OrgSettingResponse>> Handle(UpdateOrgSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.OrgSettingRepository.FindByIdAsync(request.Request.Id, cancellationToken).ConfigureAwait(false);

        if (setting == null)
        {
            return AppResponses.Failure<OrgSettingResponse>("Tenant setting not found.");
        }

        // Check if the key was changed and if it conflicts with an existing one
        if (setting.Key != request.Request.Key)
        {
            var existingWithKey = await unitOfWork.OrgSettingRepository.FirstOrDefaultAsync(x => x.Key == request.Request.Key && x.Id != request.Request.Id, cancellationToken).ConfigureAwait(false);
            if (existingWithKey != null)
            {
                return AppResponses.Failure<OrgSettingResponse>($"A setting with key '{request.Request.Key}' already exists.");
            }
            setting.Key = request.Request.Key;
        }

        // Only update the value if it's not the obscured mask "***"
        if (request.Request.Value != "***")
        {
            setting.Value = OrgSettingMapping.IsSensitiveKey(request.Request.Key)
                ? encryptionService.Encrypt(request.Request.Value)
                : request.Request.Value;
        }

        setting.Description = request.Request.Description;


        await unitOfWork.OrgSettingRepository.UpdateAsync(setting, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var response = setting.ToResponse();
        return AppResponses.Success("Tenant setting updated successfully.", response);
    }
}
