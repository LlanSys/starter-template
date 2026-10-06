namespace ST.SharedKernel.Features.Shared.Lookups.Dtos;

public sealed record LookupCatalogTypeResponse(
    int Id,
    string Key,
    string Label,
    string Description,
    bool IsActive);
