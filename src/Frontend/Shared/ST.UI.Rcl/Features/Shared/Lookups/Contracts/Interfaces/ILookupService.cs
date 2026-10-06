using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Lookups.Dtos;

namespace ST.UI.Rcl.Features.Shared.Lookups.Contracts.Interfaces;

public interface ILookupService
{
    Task<AppResponse<IReadOnlyList<LookupCatalogTypeResponse>>> GetCatalogTypesAsync();

    Task<AppResponse<IReadOnlyList<LookupResponse>>> GetAsync(string lookupType);

    Task<AppResponse<LookupResponse>> CreateAsync(string lookupType, CreateLookupRequest request);

    Task<AppResponse<LookupResponse>> UpdateAsync(string lookupType, int id, UpdateLookupRequest request);

    Task<AppResponse<bool>> DeleteAsync(string lookupType, int id);
}
