using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using ST.Infrastructure.Logging;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class ClaimsService(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager, ILogger<ClaimsService> logger, TimeProvider timeProvider) : IClaimsService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<AppRole> _roleManager = roleManager;
    private readonly ILogger<ClaimsService> _logger = logger;

    public async Task<bool> AddUserClaimAsync(AppUser user, Claim claim)
    {
        try
        {
            var result = await _userManager.AddClaimAsync(user, claim).ConfigureAwait(false);
            if (result.Succeeded)
            {
                ServiceLogDefinitions.LogClaimAdded(_logger, claim.Type, claim.Value, user.Id);

                return true;
            }

            ServiceLogDefinitions.LogFailedToAddClaim(_logger, claim.Type, claim.Value, user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

            return false;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorAddingClaim(_logger, user.Id, ex);
            throw;
        }
    }

    public async Task<List<Claim>> GetUserClaimsAsync(AppUser user, Guid? sessionId = null)
    {
        try
        {
            var claims = new List<Claim>
            {
                // Standard Identity
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
                new(JwtRegisteredClaimNames.Iat, timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),

                new("tenant_id", user.TenantId.ToString()),
                new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}"),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new("mfa_enrolled", user.TwoFactorEnabled ? "true" : "false"),
            };

            if (sessionId.HasValue && sessionId.Value != Guid.Empty)
                claims.Add(new("session_id", sessionId.Value.ToString()));

            if (user.RequiresAdministratorSetup)
                return claims;

            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

            foreach (var roleName in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));

                // 2. DYNAMICALLY fetch permissions linked to this role from the DB
                var role = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role).ConfigureAwait(false);

                    // Permissions are stored as claims with type "permission"
                    var permissions = roleClaims.Where(c => c.Type == "permission");
                    claims.AddRange(permissions);
                }
            }

            var directClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
            claims.AddRange(directClaims.Where(c => c.Type is "permission" or "platform_staff" or "demo_workspace" or "demo_actor_id" or "demo_readonly" or "support_grant_id" or "support_actor_id"));

            return [.. claims
                .GroupBy(static claim => $"{claim.Type}:{claim.Value}", StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())];
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGetUserClaimsError(_logger, user.Id, ex);
            throw;
        }

    }

    public async Task<List<Claim>> GetUserClaimsCombinedAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Iat, timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),

            new("tenant_id", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        };


        // Get permissions from the User's ROLES
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role).ConfigureAwait(false);
                claims.AddRange(roleClaims.Where(c => c.Type == "permission"));
            }
        }

        // Get permissions assigned DIRECTLY to the USER (the "Permission 5" case)
        var directClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
        claims.AddRange(directClaims.Where(c => c.Type is "permission" or "platform_staff" or "demo_workspace" or "demo_actor_id" or "demo_readonly" or "support_grant_id" or "support_actor_id"));

        // Return a distinct list (so Permission 1 isn't added twice if it's in both)
        return [.. claims.GroupBy(c => c.Value).Select(g => g.First())];
    }

    public async Task<bool> RemoveUserClaimAsync(AppUser user, Claim claim)
    {
        try
        {
            var result = await _userManager.RemoveClaimAsync(user, claim).ConfigureAwait(false);
            if (result.Succeeded)
            {
                ServiceLogDefinitions.LogClaimRemoved(_logger, claim.Type, claim.Value, user.Id);

                return true;
            }

            ServiceLogDefinitions.LogFailedToRemoveClaim(_logger, claim.Type, claim.Value, user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

            return false;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorRemovingClaim(_logger, user.Id, ex);
            throw;
        }
    }

    public async Task<bool> UpdateUserClaimAsync(AppUser user, Claim existingClaim, Claim newClaim)
    {
        try
        {
            // Remove old claim and add new one
            var removeResult = await _userManager.RemoveClaimAsync(user, existingClaim).ConfigureAwait(false);
            if (!removeResult.Succeeded)
            {
                ServiceLogDefinitions.LogFailedToRemoveExistingClaim(_logger, user.Id);

                return false;
            }

            var addResult = await _userManager.AddClaimAsync(user, newClaim).ConfigureAwait(false);
            if (!addResult.Succeeded)
            {
                // Try to rollback by adding the old claim back
                await _userManager.AddClaimAsync(user, existingClaim).ConfigureAwait(false);

                ServiceLogDefinitions.LogFailedToAddNewClaimRolledBack(_logger, user.Id);

                return false;
            }

            var oldClaim = $"{existingClaim.Type}:{existingClaim.Value}";
            var replacementClaim = $"{newClaim.Type}:{newClaim.Value}";
            ServiceLogDefinitions.LogUpdatedClaim(_logger, user.Id, oldClaim, replacementClaim);

            return true;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorUpdatingClaim(_logger, user.Id, ex);
            throw;
        }
    }


}
