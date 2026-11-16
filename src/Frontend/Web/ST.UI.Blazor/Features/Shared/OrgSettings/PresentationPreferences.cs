using System.Globalization;
using ST.SharedKernel.Features.Shared.OrgSettings;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
namespace ST.UI.Blazor.Features.Shared.OrgSettings;

public sealed class PresentationPreferences
{
    private TimeZoneInfo _zone = TimeZoneInfo.Utc;
    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-GB");
    public PresentationSettingsResponse Settings { get; private set; } = PresentationSettingsResponse.Default;
    public int PageSize => Settings.PageSize;
    public string TimeZoneLabel => Settings.TimeZone;
    public event Action? Changed;

    public void Apply(PresentationSettingsResponse settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var zone = OrganizationSettingCatalog.EffectiveValue(OrganizationSettingCatalog.TimeZone, settings.TimeZone);
        var culture = OrganizationSettingCatalog.EffectiveValue(OrganizationSettingCatalog.RegionalFormat, settings.RegionalFormat);
        var date = OrganizationSettingCatalog.EffectiveValue(OrganizationSettingCatalog.DateFormat, settings.DateFormat);
        var time = OrganizationSettingCatalog.EffectiveValue(OrganizationSettingCatalog.TimeFormat, settings.TimeFormat);
        var pageSize = OrganizationSettingCatalog.IsValid(OrganizationSettingCatalog.PageSize, settings.PageSize.ToString(CultureInfo.InvariantCulture)) ? settings.PageSize : 25;
        Settings = new(zone, culture, date, time, pageSize);
        _zone = TimeZoneInfo.FindSystemTimeZoneById(zone);
        _culture = CultureInfo.GetCultureInfo(culture);
        Changed?.Invoke();
    }

    public string Format(DateTimeOffset? value)
    {
        if (value is null)
        {
            return "—";
        }
        var local = TimeZoneInfo.ConvertTime(value.Value, _zone);
        var timeFormat = Settings.TimeFormat == "12" ? "hh:mm tt" : "HH:mm";
        return local.ToString(Settings.DateFormat + " " + timeFormat + " zzz", _culture);
    }

    public string Format(DateTime? value)
    {
        // API DateTime contracts in this template represent UTC, including unspecified JSON values.
        return Format(value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)));
    }

    public string FormatDate(DateOnly value)
    {
        return value.ToString(Settings.DateFormat, _culture);
    }

    public string Number(decimal value, string format = "N2")
    {
        return value.ToString(format, _culture);
    }
}
