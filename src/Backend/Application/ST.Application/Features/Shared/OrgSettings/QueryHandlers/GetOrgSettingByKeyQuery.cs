using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using MediatR;

namespace ST.Application.Features.Shared.OrgSettings.QueryHandlers;

public record GetOrgSettingByKeyQuery(string Key)
    : IRequest<AppResponse<OrgSettingResponse>>, ICachableRequest
{
    public string CacheGroup => "tenant-settings";
    public string Discriminator => Key;
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
