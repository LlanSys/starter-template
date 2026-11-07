using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Menus.Entities;

public sealed class MenuItem : BaseEntity, ISoftDeletable
{
    public Guid? ParentId { get; private set; }

    public string Key { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public string Placement { get; private set; } = string.Empty;
    public string? RequiredPermissionKey { get; private set; }
    public string? RequiredModule { get; private set; }
    public string[] RequiredRoleNames { get; private set; } = [];
    public string[] RequiredPermissionKeys { get; private set; } = [];

    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private MenuItem()
    {
    }

    public static MenuItem Create(
        Guid? parentId,

        string key,
        string title,
        string description,
        string url,
        string icon,
        string placement,
        string? requiredPermissionKey,
        string? requiredModule,
        int displayOrder,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(icon);
        ArgumentException.ThrowIfNullOrWhiteSpace(placement);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new MenuItem
        {
            Id = Guid.CreateVersion7(),
            ParentId = parentId,

            Key = NormalizeKey(key),
            Title = title.Trim(),
            Description = description.Trim(),
            Url = url.Trim(),
            Icon = icon.Trim(),
            Placement = placement.Trim(),
            RequiredPermissionKey = NormalizeOptional(requiredPermissionKey),
            RequiredModule = NormalizeOptional(requiredModule),
            DisplayOrder = displayOrder,
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(
        Guid? parentId,

        string key,
        string title,
        string description,
        string url,
        string icon,
        string placement,
        string? requiredPermissionKey,
        string? requiredModule,
        int displayOrder,
        bool isActive,
        string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(icon);
        ArgumentException.ThrowIfNullOrWhiteSpace(placement);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        ParentId = parentId;

        Key = NormalizeKey(key);
        Title = title.Trim();
        Description = description.Trim();
        Url = url.Trim();
        Icon = icon.Trim();
        Placement = placement.Trim();
        RequiredPermissionKey = NormalizeOptional(requiredPermissionKey);
        RequiredModule = NormalizeOptional(requiredModule);
        DisplayOrder = displayOrder;
        IsActive = isActive;
        SetUpdatedInfo(updatedBy);
    }

    public void SetVisibility(IEnumerable<string>? roles, IEnumerable<string>? permissions)
    {
        RequiredRoleNames = Normalize(roles);
        RequiredPermissionKeys = Normalize(permissions);
    }

    public bool IsVisibleTo(IReadOnlySet<string> roles, IReadOnlySet<string> permissions, bool hasFullAccess)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(permissions);
        var required = RequiredPermissionKeys.AsEnumerable();
        if (RequiredPermissionKey is not null) required = required.Append(RequiredPermissionKey);
        return (RequiredRoleNames.Length == 0 || RequiredRoleNames.Any(roles.Contains))
            && required.All(key => string.Equals(key, "control_plane.manage", StringComparison.OrdinalIgnoreCase)
                ? permissions.Contains(key) : hasFullAccess || permissions.Contains(key));
    }

    private static string[] Normalize(IEnumerable<string>? values)
        => values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        IsActive = false;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy.Trim();
        SetUpdatedInfo(deletedBy);
    }

    private static string NormalizeKey(string value)
        => value.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
