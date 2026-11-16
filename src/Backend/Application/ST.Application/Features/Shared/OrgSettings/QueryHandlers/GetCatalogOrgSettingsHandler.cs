using MediatR;
using ST.Domain.Features.Shared.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
namespace ST.Application.Features.Shared.OrgSettings.QueryHandlers;
internal sealed class GetCatalogOrgSettingsHandler(ISharedUnitOfWork unit) : IRequestHandler<GetCatalogOrgSettingsQuery, AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>> Handle(GetCatalogOrgSettingsQuery request, CancellationToken cancellationToken)
    {
        var keys = OrganizationSettingCatalog.All.Select(item => item.Key).ToArray();
        var rows = await unit.OrgSettingRepository.ListAsync(query => query.Where(row => keys.Contains(row.Key)), cancellationToken).ConfigureAwait(false);
        var map = rows.ToDictionary(row => row.Key, StringComparer.Ordinal);
        IReadOnlyList<CatalogOrgSettingResponse> result = OrganizationSettingCatalog.All.Select(definition =>
        {
            map.TryGetValue(definition.Key, out var row);
            return new CatalogOrgSettingResponse(definition, OrganizationSettingCatalog.EffectiveValue(definition.Key, row?.Value),
                row is not null, row is null ? null : Convert.ToBase64String(row.RowVersion));
        }).ToArray();

        return AppResponses.Success(result);
    }
}
