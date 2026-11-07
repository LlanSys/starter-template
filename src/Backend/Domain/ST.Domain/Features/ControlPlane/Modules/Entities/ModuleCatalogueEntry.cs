using ST.Domain.Shared.Contracts.Common;
namespace ST.Domain.Features.ControlPlane.Modules.Entities;
public sealed class ModuleCatalogueEntry : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string ModuleKey { get; set; }
    public required string Key { get; set; }
    public required string Title { get; set; }
    public ModuleCatalogueKind Kind { get; set; }
    public string Path { get; set; } = "";
    public string? ParentKey { get; set; }
    public string Icon { get; set; } = "Menu";
    public string Placement { get; set; } = "Sidebar";
    public int DisplayOrder { get; set; }
    public bool ShowInNavigation { get; set; } = true;
    public string[] PermissionKeys { get; set; } = [];
    public string[] RoleNames { get; set; } = [];
    public string? FeatureFlag { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
