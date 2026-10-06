namespace ST.SharedKernel.Features.IAM.ReferenceData.Dtos;

public sealed record CatalogOptionResponse(
    string Key,
    string Label,
    string? Description = null,
    string? ParentKey = null);
