using System.Security.Claims;
using ST.Application.Features.IAM.Permissions.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infrastructure.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateRolePermissions(
    RoleManager<AppRole> roleManager,
    IamDBContext context,
    ILogger<UpdateRolePermissions> logger)
    : IRequestHandler<UpdateRolePermissionsCommand, AppResponse<RolePermissionsResponse>>
{
    public async Task<AppResponse<RolePermissionsResponse>> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.FindByIdAsync(command.RoleId).ConfigureAwait(false);
            if (role is null)
            {
                return AppResponses.Failure<RolePermissionsResponse>("Role not found.");
            }

            var requestedKeys = command.Request.PermissionKeys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Select(static key => key.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var validKeys = await context.Permissions
                .AsNoTracking()
                .Where(permission => requestedKeys.Contains(permission.Key) && permission.IsActive)
                .Where(permission => permission.DepartmentId == null || permission.DepartmentId == role.DepartmentId)
                .Select(static permission => permission.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var invalidKeys = requestedKeys
                .Except(validKeys, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (invalidKeys.Count > 0)
            {
                return AppResponses.Failure<RolePermissionsResponse>($"Unknown, inactive, or out-of-scope permission(s): {string.Join(", ", invalidKeys)}.");
            }

            var existingClaims = (await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
                .Where(static claim => claim.Type == "permission")
                .ToList();

            foreach (var claim in existingClaims.Where(claim => !requestedKeys.Contains(claim.Value, StringComparer.OrdinalIgnoreCase)))
            {
                var removeResult = await roleManager.RemoveClaimAsync(role, claim).ConfigureAwait(false);
                if (!removeResult.Succeeded)
                {
                    return AppResponses.Failure<RolePermissionsResponse>(string.Join(", ", removeResult.Errors.Select(static error => error.Description)));
                }
            }

            var existingKeys = existingClaims
                .Select(static claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var key in requestedKeys.Where(key => !existingKeys.Contains(key)))
            {
                var addResult = await roleManager.AddClaimAsync(role, new Claim("permission", key)).ConfigureAwait(false);
                if (!addResult.Succeeded)
                {
                    return AppResponses.Failure<RolePermissionsResponse>(string.Join(", ", addResult.Errors.Select(static error => error.Description)));
                }
            }

            return AppResponses.Success(
                "Role permissions updated.",
                new RolePermissionsResponse(role.Id, role.Name ?? string.Empty, requestedKeys));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleUpdateError(logger, command.RoleId, ex);
            throw;
        }
    }
}
