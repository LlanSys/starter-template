using ST.Domain.Shared.Entities;
namespace ST.Domain.Features.Shared.OrgSettings.Entities;
public sealed class OrgSettingChange : BaseEntity
{
    public string Key { get; private set; } = "";
    public string? PreviousValue { get; private set; }
    public string? Value { get; private set; }
    public bool Reset { get; private set; }
    private OrgSettingChange() { }
    public static OrgSettingChange Record(string key, string? previousValue, string? value, bool reset, string actorId, DateTimeOffset now)
    {
        return new OrgSettingChange { Key = key, PreviousValue = previousValue, Value = value,
            Reset = reset, CreatedBy = actorId, CreatedAt = now };
    }
}
