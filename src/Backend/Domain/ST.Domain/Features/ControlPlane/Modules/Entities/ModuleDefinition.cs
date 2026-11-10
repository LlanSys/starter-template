using ST.Domain.Shared.Contracts.Common;
namespace ST.Domain.Features.ControlPlane.Modules.Entities;

public sealed class ModuleDefinition : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string[] DependencyKeys { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool IsRetired { get; private set; }
    public bool IsEssential => Key.Equals("Core", StringComparison.OrdinalIgnoreCase) || Key.Equals("IAM", StringComparison.OrdinalIgnoreCase);

    public bool ChangeAvailability(DateTimeOffset now, bool active, bool retire, string actor)
    {
        if (IsRetired || (IsEssential && (!active || retire))) return false;
        IsActive = active && !retire;
        IsRetired = retire;
        UpdatedBy = actor;
        UpdatedAt = now;
        return true;
    }
    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
