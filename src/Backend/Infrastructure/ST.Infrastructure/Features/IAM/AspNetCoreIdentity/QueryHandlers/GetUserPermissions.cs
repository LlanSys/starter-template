using ST.Application.Features.IAM.Users.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetUserPermissions(UserManager<AppUser> userManager)
    : IRequestHandler<GetUserPermissionsQuery, AppResponse<UserPermissionsResponse>>
{
    public async Task<AppResponse<UserPermissionsResponse>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
        {
            return AppResponses.Failure<UserPermissionsResponse>("User not found.");
        }

        var permissions = (await userManager.GetClaimsAsync(user).ConfigureAwait(false))
            .Where(static claim => claim.Type == "permission")
            .Select(static claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return AppResponses.Success("User permissions loaded.", new UserPermissionsResponse(user.Id, user.UserName ?? user.Email ?? user.Id, permissions));
    }
}
