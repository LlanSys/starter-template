using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.SharedKernel.Dtos.Common;

namespace ST.UI.Blazor.Features.Shared.OrgSettings.Contracts;

internal interface IOrgSettingsService
{
    Task<AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>> GetCatalogueAsync(CancellationToken ct = default);
    Task<AppResponse<PresentationSettingsResponse>> GetPresentationAsync(CancellationToken ct = default);
    Task<AppResponse<IReadOnlyList<OrgSettingChangeResponse>>> GetHistoryAsync(CancellationToken ct = default);
    Task<AppResponse<bool>> SaveAsync(string key, SaveCatalogOrgSettingRequest request, CancellationToken ct = default);
    Task<AppResponse<bool>> ResetAsync(string key, SaveCatalogOrgSettingRequest request, CancellationToken ct = default);
    Task<AppResponse<IEnumerable<OrgSettingResponse>>> GetOrgSettingsAsync(CancellationToken ct = default);
    Task<AppResponse<OrgSettingResponse>> GetOrgSettingByKeyAsync(string key, CancellationToken ct = default);
    Task<AppResponse<OrgSettingResponse>> CreateOrgSettingAsync(CreateOrgSettingRequest request, CancellationToken ct = default);
    Task<AppResponse<OrgSettingResponse>> UpdateOrgSettingAsync(UpdateOrgSettingRequest request, CancellationToken ct = default);
    Task<AppResponse<bool>> DeleteOrgSettingAsync(Guid id, CancellationToken ct = default);
}
