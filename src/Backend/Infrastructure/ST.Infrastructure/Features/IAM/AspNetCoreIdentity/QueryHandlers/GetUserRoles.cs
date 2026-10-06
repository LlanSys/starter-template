using ST.Application.Features.IAM.Users.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetUserRoles(UserManager<AppUser> userManager)
    : IRequestHandler<GetUserRolesQuery, AppResponse<UserRolesResponse>>
{
    public async Task<AppResponse<UserRolesResponse>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
        {
            return AppResponses.Failure<UserRolesResponse>("User not found.");
        }

        var roles = (await userManager.GetRolesAsync(user).ConfigureAwait(false))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return AppResponses.Success("User roles loaded.", new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, roles));
    }
}
