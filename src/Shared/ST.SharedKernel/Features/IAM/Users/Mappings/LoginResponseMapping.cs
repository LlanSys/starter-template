using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.SharedKernel.Features.IAM.Users.Mappings;

public static class LoginResponseMapping
{
    // A neutral fallback when a successful login is followed by an unavailable current-user endpoint.
    public static CurrentUserResponse ToCurrentUserResponse(this LoginResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var user = response.UserInfo ?? throw new ArgumentException("Account information is required.", nameof(response));
        var permissions = response.UserClaims?
            .Where(static claim => string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase))
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        return new CurrentUserResponse(
            AppUserId: user.Id,
            TenantId: user.TenantId,
            UserName: user.Username,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber ?? string.Empty,
            EmailConfirmed: user.EmailConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            IsAuthenticated: response.IsAuthenticated,
            LastLoginAt: user.LastLoginAt,
            Roles: [.. user.Roles],
            Permissions: permissions,
            SessionId: response.SessionId,
            MfaEnrollmentRequired: response.MfaEnrollmentRequired,
            ProfilePictureUrl: user.ProfilePictureUrl);
    }
}
