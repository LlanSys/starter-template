namespace ST.Domain.Features.IAM.ReferenceData.Entities;

public sealed class MenuRoute : ReferenceCatalogEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string PlacementKey { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private MenuRoute()
    {
    }

    public static MenuRoute Create(string key, string label, string url, string placementKey, string description, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(placementKey);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new MenuRoute
        {
            Id = Guid.CreateVersion7(),
            Key = key.Trim().ToLowerInvariant().Replace(' ', '-'),
            Label = label.Trim(),
            Url = url.Trim(),
            PlacementKey = placementKey.Trim(),
            Description = description.Trim(),
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(DateTimeOffset now, string label, string url, string placementKey, string description, bool isActive, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(placementKey);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Label = label.Trim();
        Url = url.Trim();
        PlacementKey = placementKey.Trim();
        Description = description.Trim();
        IsActive = isActive;
        SetUpdatedInfo(updatedBy, now);
    }
}
