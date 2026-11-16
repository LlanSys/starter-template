using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
namespace ST.Application.Features.Shared.OrgSettings.QueryHandlers;
public sealed record GetCatalogOrgSettingsQuery : IRequest<AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>>, ICachableRequest
{
    public string CacheGroup => "tenant-settings";
    public string Discriminator => "catalogue-v1";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
