using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ST.Application.Features.IAM.Users.Mappings;

public static class AppUserMapping
{
    /// <summary>
    /// Maps an AppUser entity to an AppUserResponse DTO.
    /// Note: Roles must be loaded/passed explicitly as IdentityUser doesn't hold them in a navigation property by default.
    /// </summary>
    public static AppUserResponse ToAppUserResponse(this AppUser user, IEnumerable<string>? roles = null)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        return new AppUserResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}",
            user.PhoneNumber,
            user.NationalId,
            user.Gender.ToString(),
            ProfilePictureUrlMapping.ToCurrentUserRoute(user.ProfilePictureUrl),
            user.IsActive,
            user.TwoFactorEnabled,
            user.RequirePasswordChange,
            user.CreatedAt,
            user.LastLoginAt,
            roles?.ToList() ?? [],
            user.TenantId,
            user.EmployeeId,
            user.CustomerId
        );
    }

    /// <summary>
    /// Projection expression for use with IQueryable to ensure efficient database queries.
    /// </summary>
    public static Expression<Func<AppUser, AppUserResponse>> AsResponse => user =>
        new AppUserResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.FirstName + " " + user.LastName,
            user.PhoneNumber,
            user.NationalId,
            user.Gender.ToString(),
            ProfilePictureUrlMapping.ToCurrentUserRoute(user.ProfilePictureUrl),
            user.IsActive,
            user.TwoFactorEnabled,
            user.RequirePasswordChange,
            user.CreatedAt,
            user.LastLoginAt,
            new List<string>(), // Roles are usually handled via UserManager after the initial query
            user.TenantId,
            user.EmployeeId,
            user.CustomerId
        );

    /// <summary>
    /// Helper to map a collection of users.
    /// </summary>
    public static List<AppUserResponse> ToAppUserResponseList(this IEnumerable<AppUser> users) =>
        [.. users.Select(u => u.ToAppUserResponse())];
}
