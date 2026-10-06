using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class DeleteRole(RoleManager<AppRole> roleManager, IamDBContext context, ILogger<DeleteRole> logger)
    : IRequestHandler<DeleteRoleCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.FindByIdAsync(command.RoleId).ConfigureAwait(false);
            if (role is null)
            {
                return AppResponses.Failure<bool>("Role not found.");
            }

            var assignedUserCount = await context.UserRoles
                .AsNoTracking()
                .CountAsync(userRole => userRole.RoleId == role.Id, cancellationToken)
                .ConfigureAwait(false);

            if (assignedUserCount > 0)
            {
                return AppResponses.Failure<bool>($"Role cannot be deleted because it is assigned to {assignedUserCount} user(s). Remove assignments first.");
            }

            var permissionCount = await context.RoleClaims
                .AsNoTracking()
                .CountAsync(roleClaim => roleClaim.RoleId == role.Id, cancellationToken)
                .ConfigureAwait(false);

            if (permissionCount > 0)
            {
                return AppResponses.Failure<bool>($"Role cannot be deleted because it has {permissionCount} permission claim(s). Remove permissions first.");
            }

            role.MarkAsDeleted(command.DeletedBy);
            var result = await roleManager.UpdateAsync(role).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("Role deleted.", true)
                : AppResponses.Failure<bool>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleDeleteError(logger, command.RoleId, ex);
            throw;
        }
    }
}
