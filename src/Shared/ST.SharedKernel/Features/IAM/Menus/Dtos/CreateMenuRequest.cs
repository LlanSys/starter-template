namespace ST.SharedKernel.Features.IAM.Menus.Dtos;

public sealed record CreateMenuRequest
{
    public Guid? ParentId { get; init; }

    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Icon { get; init; } = "Menu";
    public string Placement { get; init; } = "AdminCenter";
    public string? RequiredPermissionKey { get; init; }
    public string? RequiredModule { get; init; }
    public int DisplayOrder { get; init; }
}
