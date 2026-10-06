using System.Globalization;

namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record PermissionSearchRequest(
    string? GlobalSearch = null,
    string? Context = null,
    string? Resource = null,

    bool? IsActive = null,
    Guid? Cursor = null,
    int PageSize = 50)
{
    public string BuildQueryString()
    {
        var parameters = new List<string>(6);
        Add(parameters, nameof(GlobalSearch), GlobalSearch);
        Add(parameters, nameof(Context), Context);
        Add(parameters, nameof(Resource), Resource);

        Add(parameters, nameof(IsActive), IsActive?.ToString());
        Add(parameters, nameof(Cursor), Cursor?.ToString());
        Add(parameters, nameof(PageSize), PageSize.ToString(CultureInfo.InvariantCulture));

        return parameters.Count == 0 ? string.Empty : $"?{string.Join('&', parameters)}";
    }

    private static void Add(List<string> parameters, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parameters.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
    }
}
