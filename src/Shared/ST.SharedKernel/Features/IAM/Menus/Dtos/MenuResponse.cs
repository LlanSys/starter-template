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
    IReadOnlyList<MenuResponse> Children);
