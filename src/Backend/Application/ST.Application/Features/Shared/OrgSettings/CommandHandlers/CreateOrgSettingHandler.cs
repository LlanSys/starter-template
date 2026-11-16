using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;
internal sealed class CreateOrgSettingHandler : IRequestHandler<CreateOrgSettingCommand, AppResponse<OrgSettingResponse>>
{
    public Task<AppResponse<OrgSettingResponse>> Handle(CreateOrgSettingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AppResponses.Failure<OrgSettingResponse>("Use the supported organization settings catalogue to save settings."));
    }
}
