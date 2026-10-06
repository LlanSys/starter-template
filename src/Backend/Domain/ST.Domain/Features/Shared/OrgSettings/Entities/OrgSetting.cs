using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.Shared.OrgSettings.Entities;

public class OrgSetting : BaseEntity
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public string? Description { get; set; }

    // For EF Core
    protected OrgSetting() { }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public OrgSetting(string key, string value, string createdBy, string? description = null)
    {
        Key = key;
        Value = value;
        Description = description;
        CreatedBy = createdBy;
    }
}
