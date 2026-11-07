using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Permissions.Entities;

public sealed class Permission : BaseEntity, ISoftDeletable
{

    public string ModuleKey { get; private set; } = "iam";
    public void SetModule(string moduleKey) { ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey); ModuleKey = moduleKey.Trim().ToLowerInvariant(); }
    public string Key { get; private set; } = string.Empty;
    public string Context { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private Permission()
    {
    }

    public static Permission Create(

        string context,
        string resource,
        string action,
        string description,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var normalizedResource = NormalizeSegment(resource);
        var normalizedAction = NormalizeSegment(action);

        return new Permission
        {
            Id = Guid.CreateVersion7(),

            Context = context.Trim(),
            Resource = normalizedResource,
            Action = normalizedAction,
            Key = $"{normalizedResource}.{normalizedAction}",
            Description = description.Trim(),
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(DateTimeOffset now, string context, string resource, string action, string description, bool isActive, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        var normalizedResource = NormalizeSegment(resource);
        var normalizedAction = NormalizeSegment(action);

        Context = context.Trim();
        Resource = normalizedResource;
        Action = normalizedAction;
        Key = $"{normalizedResource}.{normalizedAction}";
        Description = description.Trim();
        IsActive = isActive;
        SetUpdatedInfo(updatedBy, now);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        IsActive = false;
        DeletedAt = now;
        DeletedBy = deletedBy.Trim();
        SetUpdatedInfo(deletedBy, now);
    }

    private static string NormalizeSegment(string value)
        => value.Trim().ToLowerInvariant().Replace(' ', '_');
}
