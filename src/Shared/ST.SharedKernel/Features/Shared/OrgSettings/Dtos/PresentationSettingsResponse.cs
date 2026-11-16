namespace ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
public sealed record PresentationSettingsResponse(string TimeZone, string RegionalFormat, string DateFormat, string TimeFormat, int PageSize)
{
    public static PresentationSettingsResponse Default { get; } = new("UTC", "en-GB", "dd MMM yyyy", "24", 25);
}
