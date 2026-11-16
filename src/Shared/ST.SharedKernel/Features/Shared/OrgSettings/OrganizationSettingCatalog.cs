namespace ST.SharedKernel.Features.Shared.OrgSettings;

public static class OrganizationSettingCatalog
{
    public const string TimeZone = "presentation.time-zone";
    public const string RegionalFormat = "presentation.regional-format";
    public const string DateFormat = "presentation.date-format";
    public const string TimeFormat = "presentation.time-format";
    public const string PageSize = "presentation.page-size";

    public static IReadOnlyList<OrgSettingDefinition> All { get; } = Array.AsReadOnly<OrgSettingDefinition>(
    [
        new(TimeZone, "Time Zone", "Time zone used to display timestamps. Stored instants remain UTC.", "UTC",
            Array.AsReadOnly(TimeZoneInfo.GetSystemTimeZones()
                .Select(zone => zone.HasIanaId ? zone.Id :
                    TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var iana) ? iana : "UTC")
                .Select(id => new OrgSettingOption(id, id.Replace('_', ' ')))
                .Prepend(new OrgSettingOption("UTC", "UTC"))
                .DistinctBy(option => option.Value).ToArray())),
        new(RegionalFormat, "Regional Format", "Formatting for dates and numbers; this does not translate the interface.", "en-GB",
            Choices("en-GB", "English (UK)", "en-US", "English (US)", "en-KE", "English (Kenya)", "fr-FR", "French (France)")),
        new(DateFormat, "Date Format", "Presentation of calendar dates and the date part of timestamps.", "dd MMM yyyy",
            Choices("dd MMM yyyy", "14 Nov 2026", "yyyy-MM-dd", "2026-11-14", "dd/MM/yyyy", "14/11/2026", "MM/dd/yyyy", "11/14/2026")),
        new(TimeFormat, "Time Format", "Presentation of the time part of timestamps.", "24",
            Choices("24", "24-Hour", "12", "12-Hour")),
        new(PageSize, "Default Rows Per Page", "Initial page size for administration and operational listings.", "25",
            Choices("10", "10", "25", "25", "50", "50", "100", "100"))
    ]);

    public static OrgSettingDefinition? Find(string? key)
    {
        return All.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal));
    }

    public static bool IsValid(string? key, string? value)
    {
        return Find(key)?.Options.Any(option => string.Equals(option.Value, value, StringComparison.Ordinal)) == true;
    }

    public static string EffectiveValue(string key, string? value)
    {
        return IsValid(key, value) ? value! : Find(key)!.DefaultValue;
    }

    private static IReadOnlyList<OrgSettingOption> Choices(params string[] pairs)
    {
        return Array.AsReadOnly(Enumerable.Range(0, pairs.Length / 2)
            .Select(index => new OrgSettingOption(pairs[index * 2], pairs[index * 2 + 1])).ToArray());
    }
}
