using ST.SharedKernel.Features.Shared.OrgSettings;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.UI.Blazor.Features.Shared.OrgSettings;
namespace ST.Tests.Unit.SharedPresentation;
public sealed class PresentationPreferencesTests
{
    [Fact]
    public void UtcDefaultsDoNotChangeTheInjectedClockOrCalendarDates()
    {
        TimeProvider clock = new FixedClock();
        var now = clock.GetUtcNow();
        var preferences = new PresentationPreferences();
        Assert.Equal("14 Nov 2026 23:30 +00:00", preferences.Format(now));
        preferences.Apply(PresentationSettingsResponse.Default with { TimeZone = "Africa/Nairobi", TimeFormat = "12" });
        Assert.Equal("15 Nov 2026 02:30 am +03:00", preferences.Format(now), ignoreCase: true);
        Assert.Equal(now, clock.GetUtcNow());
        Assert.Equal("14 Nov 2026", preferences.FormatDate(new DateOnly(2026, 11, 14)));
    }

    [Fact]
    public void ZoneConversionRespectsDaylightSavingAndOffsetInstants()
    {
        var preferences = new PresentationPreferences();
        preferences.Apply(PresentationSettingsResponse.Default with { TimeZone = "America/New_York" });
        Assert.EndsWith("-05:00", preferences.Format(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero)), StringComparison.Ordinal);
        Assert.EndsWith("-04:00", preferences.Format(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero)), StringComparison.Ordinal);
        Assert.Equal(preferences.Format(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero)),
            preferences.Format(new DateTimeOffset(2026, 7, 10, 15, 0, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void CatalogueRejectsArbitraryKeysValuesAndSupportsCrossPlatformTimeZones()
    {
        Assert.True(OrganizationSettingCatalog.IsValid(OrganizationSettingCatalog.TimeZone, "Africa/Nairobi"));
        Assert.False(OrganizationSettingCatalog.IsValid(OrganizationSettingCatalog.TimeZone, "Eastern Standard Time"));
        Assert.False(OrganizationSettingCatalog.IsValid("JwtSettings:SecretKey", "anything"));
        Assert.False(OrganizationSettingCatalog.IsValid(OrganizationSettingCatalog.PageSize, "100000"));
        var preferences = new PresentationPreferences();
        preferences.Apply(PresentationSettingsResponse.Default with { RegionalFormat = "fr-FR", PageSize = 50 });
        Assert.Contains(",89", preferences.Number(1234567.89m), StringComparison.Ordinal);
        Assert.Equal(50, preferences.PageSize);
        preferences.Apply(PresentationSettingsResponse.Default);
        Assert.Equal("1,234,567.89", preferences.Number(1234567.89m));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 11, 14, 23, 30, 0, TimeSpan.Zero);
        }
    }
}
