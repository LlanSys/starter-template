using ST.Domain.Shared.Entities;
namespace ST.Domain.Features.IAM.Menus.Entities;
public sealed class NavigationOverride : BaseEntity, ST.Domain.Shared.Contracts.Common.ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public void MarkAsDeleted(string actor) { ArgumentException.ThrowIfNullOrWhiteSpace(actor); IsDeleted = true; DeletedAt = DateTimeOffset.UtcNow; DeletedBy = actor; }
    public required string PageKey { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = "Menu";
    public string Placement { get; set; } = "Sidebar";
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool UsePlatformDefaults { get; set; }
    public bool IsActive { get; set; } = true;
    public string[] RoleNames { get; set; } = [];
    public string[] PermissionKeys { get; set; } = [];
    public int Version { get; set; } = 1;
}
