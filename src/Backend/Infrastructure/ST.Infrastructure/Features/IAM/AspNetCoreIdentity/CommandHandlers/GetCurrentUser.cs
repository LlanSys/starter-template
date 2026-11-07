using Microsoft.Extensions.DependencyInjection;
using ST.SharedKernel.Extensions;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class GetCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    UserManager<AppUser> userManager,
    IOptions<MfaSettings> mfaSettings,
    ILogger<GetCurrentUser> logger, ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy policy) : IRequestHandler<GetCurrentUserCommand, AppResponse<CurrentUserResponse>>
{
    private readonly MfaSettings _mfaSettings = mfaSettings.Value;

    public async Task<AppResponse<CurrentUserResponse>> Handle(GetCurrentUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return AppResponses.Failure<CurrentUserResponse>("User not found.");

            var appUser = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (appUser == null)
                return AppResponses.Failure<CurrentUserResponse>("User not found.");

            var roles = await userManager.GetRolesAsync(appUser).ConfigureAwait(false);
            var rolesList = roles.ToList();

            var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(appUser).ConfigureAwait(false);
            var emailConfirmed = await userManager.IsEmailConfirmedAsync(appUser).ConfigureAwait(false);
            await userManager.IsPhoneNumberConfirmedAsync(appUser).ConfigureAwait(false);

            bool? isAuthenticated = httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            var permissions = httpContextAccessor.HttpContext?.User.Claims
                .Where(static claim => string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase))
                .Select(static claim => claim.Value)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            var supportId = Guid.TryParse(httpContextAccessor.HttpContext!.User.FindFirstValue("support_grant_id"),out var sg) ? (Guid?)sg : null;
            var support = supportId.HasValue ? await httpContextAccessor.HttpContext.RequestServices.GetRequiredService<ST.Domain.Features.ControlPlane.Contracts.IControlPlaneUnitOfWork>().SupportGrants.FindByIdAsync(supportId.Value,cancellationToken).ConfigureAwait(false) : null;
            if (supportId.HasValue) rolesList.Clear(); // The approved grant, not mutable proxy roles, owns support authority.
            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            permissions = permissions.Where(key => key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) || access.PermissionAvailable(key)).ToList();
            var sessionId = httpContextAccessor.HttpContext?.User.FindFirstValue("session_id");
            var mfaEnrollmentRequired = !httpContextAccessor.HttpContext!.Items.ContainsKey("TrustedWorkspaceProxy") && IsMfaEnrollmentRequired(twoFactorEnabled, rolesList);

            return AppResponses.Success("CurrentUser", new CurrentUserResponse(
                userId,
                appUser.TenantId,
                appUser.UserName ?? string.Empty,
                appUser.Email ?? string.Empty,
                appUser.FirstName ?? string.Empty,
                appUser.LastName ?? string.Empty,
                appUser.PhoneNumber ?? string.Empty,
                emailConfirmed,
                twoFactorEnabled,
                isAuthenticated,
                appUser.LastLoginAt,
                rolesList,
                permissions,
                sessionId,
                mfaEnrollmentRequired,
                ProfilePictureUrlMapping.ToCurrentUserRoute(appUser.ProfilePictureUrl))
            {
                SupportGrantId = supportId, IsReadOnlySupport = support?.ReadOnly ?? false, SupportExpiresAt = support?.ExpiresAt,
                WorkspaceName = support is null ? null : (await httpContextAccessor.HttpContext.RequestServices.GetRequiredService<ST.Domain.Features.ControlPlane.Contracts.IControlPlaneUnitOfWork>().Tenants.FindByIdAsync(support.WorkspaceId,cancellationToken).ConfigureAwait(false))?.DisplayName,
                UnavailablePermissions = access.Catalogue.Where(x => x.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Permission && !access.PermissionAvailable(x.Key)).Select(x => x.Key).Concat(access.PrivatePermissionModules.Keys.Where(key => !access.PermissionAvailable(key))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                AvailablePages = access.AvailablePages.Where(page => !string.IsNullOrEmpty(page.Path)
                    && page.PermissionKeys.All(key => access.PermissionAvailable(key) && (rolesList.Contains("System Administrator", StringComparer.OrdinalIgnoreCase) || permissions.Contains(key, StringComparer.OrdinalIgnoreCase))))
                    .Select(page => page.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                EnabledModules = access.Modules.ToArray(),
                IsPlatformStaff = ST.SharedKernel.Features.ControlPlane.PlatformAccess.IsStaff(httpContextAccessor.HttpContext!.User),
                IsReadOnlyDemo = httpContextAccessor.HttpContext!.User.HasClaim("demo_readonly", "true"),
                IsDemoWorkspace = httpContextAccessor.HttpContext!.User.HasClaim("demo_workspace", "true")
            });
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGetCurrentUserError(logger, ex);
            throw;
        }
    }

    private bool IsMfaEnrollmentRequired(bool twoFactorEnabled, IEnumerable<string> roles)
    {
        if (!_mfaSettings.Enabled || !_mfaSettings.EnforceEnrollment || twoFactorEnabled)
        {
            return false;
        }

        return roles.Any(role => (string.Equals(role, "Tenant Administrator", StringComparison.OrdinalIgnoreCase) || _mfaSettings.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase)));
    }
}
