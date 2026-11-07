namespace ST.Domain.Features.IAM.ReferenceData.Entities;

public sealed class MenuIcon : ReferenceCatalogEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private MenuIcon()
    {
    }

    public static MenuIcon Create(string key, string label, string description, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new MenuIcon
        {
            Id = Guid.CreateVersion7(),
            Key = key.Trim(),
            Label = label.Trim(),
            Description = description.Trim(),
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(DateTimeOffset now, string label, string description, bool isActive, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Label = label.Trim();
        Description = description.Trim();
        IsActive = isActive;
        SetUpdatedInfo(updatedBy, now);
    }
}
