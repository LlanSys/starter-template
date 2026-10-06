using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using System.Collections.Generic;

namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;

public record DeleteOrgSettingCommand(Guid Id, string UserId)
    : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("tenant-settings")
    ];
}
