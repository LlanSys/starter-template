using System.Globalization;

namespace ST.SharedKernel.Features.HR.Departments.Dtos;

public sealed record DepartmentSearchRequest(
    string? GlobalSearch = null,
    bool? IsActive = null,
    Guid? Cursor = null,
    int PageSize = 50)
{
    public string BuildQueryString()
    {
        var parameters = new List<string>(4);
        Add(parameters, nameof(GlobalSearch), GlobalSearch);
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
