using ST.Domain.Shared.Contracts.Common;
namespace ST.Domain.Features.ControlPlane.Modules.Entities;
public sealed class ModuleFeatureFlag : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    // Empty is the platform default. Nonempty rows override only the named tenant.
    public Guid ScopeTenantId { get; set; }
    public required string ModuleKey { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
