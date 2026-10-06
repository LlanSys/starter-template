using System.Linq.Expressions;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Application.Features.IAM.Users.Mappings;

public static class AppUserMapping
{
    public static AppUserResponse ToAppUserResponse(this AppUser user, IEnumerable<string>? roles = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new AppUserResponse(
            Id: user.Id,
            Username: user.UserName ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: $"{user.FirstName} {user.LastName}".Trim(),
            PhoneNumber: user.PhoneNumber,
            Email: user.Email ?? string.Empty,
            ProfilePictureUrl: ProfilePictureUrlMapping.ToCurrentUserRoute(user.ProfilePictureUrl),
            IsActive: user.IsActive,
            EmailConfirmed: user.EmailConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            RequirePasswordChange: user.RequirePasswordChange,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            Roles: roles?.ToList() ?? [],
            TenantId: user.TenantId);
    }

    // Roles are populated separately by Identity; the projection never queries each user's memberships.
    public static Expression<Func<AppUser, AppUserResponse>> AsResponse => user =>
        new AppUserResponse(
            user.Id, user.UserName ?? string.Empty, user.FirstName, user.LastName,
            (user.FirstName + " " + user.LastName).Trim(), user.PhoneNumber, user.Email ?? string.Empty,
            ProfilePictureUrlMapping.ToCurrentUserRoute(user.ProfilePictureUrl), user.IsActive,
            user.EmailConfirmed, user.TwoFactorEnabled, user.RequirePasswordChange,
            user.CreatedAt, user.LastLoginAt, new List<string>(), user.TenantId);

    public static List<AppUserResponse> ToAppUserResponseList(this IEnumerable<AppUser> users)
        => [.. users.Select(user => user.ToAppUserResponse())];
}
