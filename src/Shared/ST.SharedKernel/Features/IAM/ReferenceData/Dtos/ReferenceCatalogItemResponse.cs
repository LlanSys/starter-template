namespace ST.SharedKernel.Features.IAM.ReferenceData.Dtos;

public sealed record ReferenceCatalogItemResponse(
    Guid Id,
    string CatalogType,
    string Key,
    string Label,
    string Description,
    string? ParentKey,
    string? Url,
    bool IsActive);
