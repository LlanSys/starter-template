using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using MediatR;
using System.Collections.Generic;

namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;

public record UpdateOrgSettingCommand(UpdateOrgSettingRequest Request, string UserId)
    : IRequest<AppResponse<OrgSettingResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("tenant-settings")
    ];
}
