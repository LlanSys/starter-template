using System.Globalization;

namespace ST.SharedKernel.Features.HR.Employees.Dtos;

public sealed record EmployeeSearchRequest(
    string? GlobalSearch = null,
    Guid? DepartmentId = null,
    Guid? ManagerId = null,
    Guid? Cursor = null,
    int PageSize = 50)
{
    public string BuildQueryString()
    {
        var parameters = new List<string>(5);
        Add(parameters, nameof(GlobalSearch), GlobalSearch);
        Add(parameters, nameof(DepartmentId), DepartmentId?.ToString());
        Add(parameters, nameof(ManagerId), ManagerId?.ToString());
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
