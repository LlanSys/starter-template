using ST.Application.Features.IAM.Users;
using ST.Application.Features.IAM.Users.Commands;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateRole(RoleManager<AppRole> roleManager, ILogger<UpdateRole> logger)
    : IRequestHandler<UpdateRoleCommand, AppResponse<AdminRoleListResponse>>
{
    public async Task<AppResponse<AdminRoleListResponse>> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.FindByIdAsync(command.RoleId).ConfigureAwait(false);
            if (role is null)
            {
                return AppResponses.Failure<AdminRoleListResponse>("Role not found.");
            }

            var name = command.Request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return AppResponses.Failure<AdminRoleListResponse>("Role name is required.");
            }

            if (RoleAccessRules.IsProtectedRole(role.Name) || RoleAccessRules.IsProtectedRole(name))
            {
                return AppResponses.Failure<AdminRoleListResponse>("This role name is reserved for workspace setup.");
            }

            role.Name = name;

            var result = await roleManager.UpdateAsync(role).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("Role updated.", new AdminRoleListResponse(role.Id, role.Name ?? name, role.NormalizedName ?? name.ToUpperInvariant(), 0))
                : AppResponses.Failure<AdminRoleListResponse>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleUpdateError(logger, command.RoleId, ex);
            throw;
        }
    }
}
