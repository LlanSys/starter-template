using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.Shared.Phone;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

internal sealed class AdminUserFormModel
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string CountryCode { get; set; } = PhoneNumberFormatter.DefaultCountryCode;

    public string PhoneNationalNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    private IReadOnlyList<string> _existingRoles = [];

    public static AdminUserFormModel From(AdminUserListResponse user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var names = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var model = new AdminUserFormModel
        {
            Username = user.UserName,
            Email = user.Email,
            FirstName = names.Length > 0 ? names[0] : user.FullName,
            LastName = names.Length > 1 ? names[1] : string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = user.Roles.Count > 0 ? user.Roles[0] : string.Empty,
            _existingRoles = [.. user.Roles]
        };

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            var country = CountryCallingCodeCatalog.FindByE164(user.PhoneNumber);
            if (country is not null)
            {
                model.CountryCode = country.DialCode;
                model.PhoneNationalNumber = user.PhoneNumber[country.DialCode.Length..];
            }
        }

        return model;
    }

    public CreateAppUserRequest ToRequest() => new(
        Username,
        Email,
        Password,
        FirstName,
        LastName,
        PhoneNumber,
        Roles: string.IsNullOrWhiteSpace(Role) ? [] : [Role]);

    public UpdateAdminUserRequest ToUpdateRequest() => new(
        Username,
        Email,
        FirstName,
        LastName,
        PhoneNumber,
        _existingRoles);
}
