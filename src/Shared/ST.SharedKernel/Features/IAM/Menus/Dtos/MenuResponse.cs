namespace ST.SharedKernel.Features.IAM.Menus.Dtos;

public sealed record MenuResponse(
    Guid Id,
    Guid? ParentId,

    string Key,
    string Title,
    string Description,
    string Url,
    string Icon,
    string Placement,
    string? RequiredPermissionKey,
    string? RequiredModule,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<MenuResponse> Children)
{
    public string? CatalogueKey { get; init; }
    public bool IsOverride { get; init; }
    public int DefinitionVersion { get; init; }
    public int OverrideVersion { get; init; }
    public IReadOnlyList<string> RequiredRoleNames { get; init; } = [];
    public IReadOnlyList<string> RequiredPermissionKeys { get; init; } = [];
}
