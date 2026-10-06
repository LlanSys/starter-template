namespace ST.SharedKernel.Features.IAM.ReferenceData.Dtos;

public sealed record ReferenceCatalogItemRequest
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentKey { get; init; }
    public string? Url { get; init; }
    public bool IsActive { get; init; } = true;
}
