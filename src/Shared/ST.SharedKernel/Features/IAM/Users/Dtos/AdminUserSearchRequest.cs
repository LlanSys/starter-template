using System.Globalization;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AdminUserSearchRequest(
    string? GlobalSearch = null,
    string? Status = null,
    string? Role = null,
    string? TwoFactorStatus = null,
    string? LinkedRecordType = null,
    Guid? EmployeeId = null,
    Guid? CustomerId = null,
    string? Cursor = null,
    int PageSize = 50)
{
    public string BuildQueryString()
    {
        var parameters = new List<string>(9);
        Add(parameters, nameof(GlobalSearch), GlobalSearch);
        Add(parameters, nameof(Status), Status);
        Add(parameters, nameof(Role), Role);
        Add(parameters, nameof(TwoFactorStatus), TwoFactorStatus);
        Add(parameters, nameof(LinkedRecordType), LinkedRecordType);
        Add(parameters, nameof(EmployeeId), EmployeeId?.ToString());
        Add(parameters, nameof(CustomerId), CustomerId?.ToString());
        Add(parameters, nameof(Cursor), Cursor);
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
