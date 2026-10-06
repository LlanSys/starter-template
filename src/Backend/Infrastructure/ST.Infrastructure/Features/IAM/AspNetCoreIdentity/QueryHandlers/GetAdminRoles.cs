using ST.Application.Features.IAM.Users.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetAdminRoles(RoleManager<AppRole> roleManager, IamDBContext context)
    : IRequestHandler<GetAdminRolesQuery, AppResponse<IReadOnlyList<AdminRoleListResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<AdminRoleListResponse>>> Handle(GetAdminRolesQuery request, CancellationToken cancellationToken)
    {
        var userCounts = await context.UserRoles
            .AsNoTracking()
            .GroupBy(static userRole => userRole.RoleId)
            .Select(static group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(static item => item.RoleId, static item => item.Count, cancellationToken)
            .ConfigureAwait(false);

        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(static role => role.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = roles
            .Select(role => new AdminRoleListResponse(
                role.Id,
                role.Name ?? string.Empty,
                role.NormalizedName ?? string.Empty,


                userCounts.GetValueOrDefault(role.Id)))
            .ToList();

        return AppResponses.Success("Roles loaded.", (IReadOnlyList<AdminRoleListResponse>)result);
    }
}
