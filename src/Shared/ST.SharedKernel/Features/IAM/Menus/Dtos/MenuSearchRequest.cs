using System.Globalization;

namespace ST.SharedKernel.Features.IAM.Menus.Dtos;

public sealed record MenuSearchRequest(
    string? GlobalSearch = null,
    string? Placement = null,
    Guid? ParentId = null,
    Guid? DepartmentId = null,
    bool? IsActive = null,
    Guid? Cursor = null,
    int PageSize = 100)
{
    public string BuildQueryString()
    {
        var parameters = new List<string>(7);
        Add(parameters, nameof(GlobalSearch), GlobalSearch);
        Add(parameters, nameof(Placement), Placement);
        Add(parameters, nameof(ParentId), ParentId?.ToString());
        Add(parameters, nameof(DepartmentId), DepartmentId?.ToString());
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
