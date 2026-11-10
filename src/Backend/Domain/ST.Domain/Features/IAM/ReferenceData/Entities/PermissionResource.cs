namespace ST.Domain.Features.IAM.ReferenceData.Entities;

public sealed class PermissionResource : ReferenceCatalogEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string ContextKey { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private PermissionResource()
    {
    }

    public static PermissionResource Create(string key, string label, string contextKey, string description, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new PermissionResource
        {
            Id = Guid.CreateVersion7(),
            Key = NormalizeSegment(key),
            Label = label.Trim(),
            ContextKey = contextKey.Trim(),
            Description = description.Trim(),
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(DateTimeOffset now, string label, string contextKey, string description, bool isActive, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Label = label.Trim();
        ContextKey = contextKey.Trim();
        Description = description.Trim();
        IsActive = isActive;
        SetUpdatedInfo(updatedBy, now);
    }

    private static string NormalizeSegment(string value) => value.Trim().ToLowerInvariant().Replace(' ', '_');
}
