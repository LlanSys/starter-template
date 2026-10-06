using ST.Application.Features.IAM.Users.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetAdminUsers(UserManager<AppUser> userManager, IamDBContext context)
    : IRequestHandler<GetAdminUsersQuery, AppResponse<PagedResponse<AdminUserListResponse, string>>>
{
    public async Task<AppResponse<PagedResponse<AdminUserListResponse, string>>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var req = request.SearchRequest;
        var pageSize = Math.Clamp(req.PageSize, 1, 50);

        var query = userManager.Users
            .AsNoTracking()
            .OrderBy(static user => user.Email)
            .ThenBy(static user => user.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.GlobalSearch))
        {
            var search = req.GlobalSearch.Trim();
            query = query.Where(user =>
                user.UserName!.Contains(search)
                || user.Email!.Contains(search)
                || user.FirstName.Contains(search)
                || user.LastName.Contains(search)
                || user.PhoneNumber!.Contains(search));
        }

        if (req.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true)
        {
            query = query.Where(static user => user.IsActive);
        }
        else if (req.Status?.Equals("Inactive", StringComparison.OrdinalIgnoreCase) == true)
        {
            query = query.Where(static user => !user.IsActive);
        }

        if (req.TwoFactorStatus?.Equals("Enabled", StringComparison.OrdinalIgnoreCase) == true)
        {
            query = query.Where(static user => user.TwoFactorEnabled);
        }
        else if (req.TwoFactorStatus?.Equals("Recommended", StringComparison.OrdinalIgnoreCase) == true)
        {
            query = query.Where(static user => !user.TwoFactorEnabled);
        }

        if (req.EmployeeId.HasValue)
        {
            query = query.Where(user => user.EmployeeId == req.EmployeeId.Value);
        }

        if (req.CustomerId.HasValue)
        {
            query = query.Where(user => user.CustomerId == req.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            var normalizedRole = userManager.NormalizeName(req.Role);
            query =
                from user in query
                join userRole in context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where role.NormalizedName == normalizedRole
                select user;
        }

        var totalRecords = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(req.Cursor))
        {
            var cursorUser = await context.Users
                .AsNoTracking()
                .Where(user => user.Id == req.Cursor)
                .Select(user => new { user.Email, user.Id })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (cursorUser is not null)
            {
                query = query.Where(user =>
                    user.Email!.CompareTo(cursorUser.Email) > 0
                    || (user.Email == cursorUser.Email
                        && user.Id.CompareTo(cursorUser.Id) > 0));
            }
        }

        var pageUsers = await query
            .OrderBy(static user => user.Email)
            .ThenBy(static user => user.Id)
            .Select(static user => new AdminUserPageRow(
                user.Id,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.RequirePasswordChange,
                user.CreatedAt,
                user.LastLoginAt))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasNextPage = pageUsers.Count > pageSize;
        if (hasNextPage)
        {
            pageUsers.RemoveAt(pageUsers.Count - 1);
        }

        var pageUserIds = pageUsers.Select(static user => user.Id).ToList();
        var roleRows = await (
                from userRole in context.UserRoles.AsNoTracking()
                join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where pageUserIds.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rolesByUser = roleRows
            .Where(static role => !string.IsNullOrWhiteSpace(role.Name))
            .GroupBy(static role => role.UserId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static role => role.Name!)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray());

        var rows = pageUsers
            .Select(user => new AdminUserListResponse(
                user.Id,
                user.UserName ?? string.Empty,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.RequirePasswordChange,
                user.CreatedAt,
                user.LastLoginAt,
                rolesByUser.GetValueOrDefault(user.Id, [])))
            .ToList();

        var nextCursor = hasNextPage ? rows[^1].Id : null;
        var paged = new PagedResponse<AdminUserListResponse, string>(
            new Collection<AdminUserListResponse>(rows),
            totalRecords,
            1,
            pageSize,
            string.IsNullOrWhiteSpace(req.Cursor),
            nextCursor);

        return AppResponses.Success("Users loaded.", paged);
    }

    private sealed record AdminUserPageRow(
        string Id,
        string? UserName,
        string FirstName,
        string LastName,
        string? Email,
        string? PhoneNumber,
        bool IsActive,
        bool EmailConfirmed,
        bool TwoFactorEnabled,
        bool RequirePasswordChange,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastLoginAt);
}
