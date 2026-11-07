using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Npgsql;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Security;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed class AdministratorInvitationService(
    IControlPlaneUnitOfWork plane, IIamUnitOfWork iam, UserManager<AppUser> users, RoleManager<AppRole> roles,
    SignInManager<AppUser> signIn, IHttpContextAccessor http, ICurrentTenantProvider tenantScope,
    ICurrentActorProvider actor, ISessionService sessions, TimeProvider timeProvider) : IAdministratorInvitationService
{
    private const string Unavailable = "This invitation is unavailable. If you already accepted it, sign in to continue setup.";

    public async Task<AppResponse<AdministratorInvitationPreview>> InspectAsync(
        InspectAdministratorInvitationRequest request, CancellationToken cancellationToken)
    {
        var invitation = await FindAsync(request.InvitationId, cancellationToken).ConfigureAwait(false);
        if (!IsValid(invitation, request.Token, timeProvider.GetUtcNow()))
        {
            return AppResponses.Failure<AdministratorInvitationPreview>(Unavailable);
        }

        var account = await users.FindByEmailAsync(invitation!.Email).ConfigureAwait(false);
        return AppResponses.Success(new AdministratorInvitationPreview(invitation.Tenant.DisplayName, invitation.Email,
            account is not null && !await IsLegacyUnclaimedAsync(account, invitation).ConfigureAwait(false),
            invitation.ExpiresAt!.Value));
    }

    public async Task<AppResponse<bool>> AcceptPasswordAsync(
        AcceptAdministratorInvitationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
        {
            return AppResponses.Failure<bool>("Enter a valid password.");
        }

        var invitation = await FindAsync(request.InvitationId, cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        if (!IsValid(invitation, request.Token, now))
        {
            return AppResponses.Failure<bool>(Unavailable);
        }

        var account = await users.FindByEmailAsync(invitation!.Email).ConfigureAwait(false);
        var legacy = account is not null && await IsLegacyUnclaimedAsync(account, invitation).ConfigureAwait(false);
        if (account is not null && !legacy)
        {
            if (!await IsEligibleAsync(account, invitation).ConfigureAwait(false))
            {
                return AppResponses.Failure<bool>(Unavailable);
            }

            var verified = await signIn.CheckPasswordSignInAsync(account, request.Password, lockoutOnFailure: true).ConfigureAwait(false);
            if (!verified.Succeeded)
            {
                return AppResponses.Failure<bool>("Use the current sign-in method for the invited account.");
            }
        }
        else if (account is null && !await HasCapacityAsync(invitation, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<bool>("The workspace has reached its account limit.");
        }

        var created = account is null;
        account ??= NewAccount(invitation, now);
        if (!await ReserveAsync(invitation, account.Id, now, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<bool>(Unavailable);
        }

        account.BeginAdministratorSetup(invitation.Id);
        IdentityResult saved;
        try
        {
            if (created)
            {
                saved = await users.CreateAsync(account, request.Password).ConfigureAwait(false);
            }
            else if (legacy)
            {
                account.EmailConfirmed = true;
                account.RequirePasswordChange = false;
                saved = await users.AddPasswordAsync(account, request.Password).ConfigureAwait(false);
            }
            else
            {
                saved = await users.UpdateAsync(account).ConfigureAwait(false);
            }
        }
        catch (DbUpdateException exception) when (IsDuplicateIdentity(exception))
        {
            return AppResponses.Failure<bool>("Account setup has already started. Sign in to continue or try the invitation again.");
        }

        if (!saved.Succeeded)
        {
            return AppResponses.Failure<bool>("Account setup could not be saved. Check the password requirements and try again.");
        }

        return await ClaimAsync(invitation, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<AppUser>> AcceptExternalAsync(Guid invitationId, string token, string provider,
        string providerKey, string email, CancellationToken cancellationToken)
    {
        var invitation = await FindAsync(invitationId, cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        if (!IsValid(invitation, token, now) || !string.Equals(invitation!.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return AppResponses.Failure<AppUser>(Unavailable);
        }

        var account = await users.FindByEmailAsync(invitation.Email).ConfigureAwait(false);
        var linked = await users.FindByLoginAsync(provider, providerKey).ConfigureAwait(false);
        if ((linked is not null && linked.Id != account?.Id)
            || (account is not null && (linked?.Id != account.Id || !await IsEligibleAsync(account, invitation).ConfigureAwait(false))))
        {
            return AppResponses.Failure<AppUser>("Use the current sign-in method for the invited account.");
        }

        var created = account is null;
        if (created && !await HasCapacityAsync(invitation, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<AppUser>("The workspace has reached its account limit.");
        }

        account ??= NewAccount(invitation, now);
        if (!await ReserveAsync(invitation, account.Id, now, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<AppUser>(Unavailable);
        }

        account.BeginAdministratorSetup(invitation.Id);
        bool saved;
        try
        {
            saved = created
            ? await iam.ExecuteInTransactionAsync(async () =>
            {
                if (!(await users.CreateAsync(account).ConfigureAwait(false)).Succeeded)
                {
                    return false;
                }

                var added = await users.AddLoginAsync(account, new UserLoginInfo(provider, providerKey, provider)).ConfigureAwait(false);
                if (!added.Succeeded)
                {
                    throw new ExternalInvitationLinkException();
                }

                await iam.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false)
            : (await users.UpdateAsync(account).ConfigureAwait(false)).Succeeded;
        }
        catch (DbUpdateException exception) when (IsDuplicateIdentity(exception))
        {
            return AppResponses.Failure<AppUser>(Unavailable);
        }
        catch (ExternalInvitationLinkException)
        {
            return AppResponses.Failure<AppUser>("The external identity could not be linked.");
        }

        if (!saved || !(await ClaimAsync(invitation, cancellationToken).ConfigureAwait(false)).IsSuccess)
        {
            return AppResponses.Failure<AppUser>(Unavailable);
        }

        return AppResponses.Success(account);
    }

    public async Task<AppResponse<bool>> CompleteAsync(CancellationToken cancellationToken)
    {
        var principal = http.HttpContext?.User;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = principal?.FindFirstValue("session_id");
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId)
            || !Guid.TryParse(sessionId, out _) || !principal.HasClaim("administrator_mfa", "true")
            || !(await sessions.IsSessionValidAsync(sessionId, userId, cancellationToken).ConfigureAwait(false)).IsSuccess)
        {
            return AppResponses.Failure<bool>(AppError.Forbidden("Verify your authenticator in this session to finish setup."));
        }

        var account = await users.FindByIdAsync(userId).ConfigureAwait(false);
        if (account is null || !account.IsActive || account.IsDeleted || !account.TwoFactorEnabled
            || account.TenantId != tenantScope.TenantId || account.AdministratorInvitationId is not Guid invitationId)
        {
            return AppResponses.Failure<bool>(Unavailable);
        }

        var invitation = await FindAsync(invitationId, cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.ClaimedUserId != account.Id
            || invitation.State is not (AdministratorInvitationState.Claimed or AdministratorInvitationState.Accepted))
        {
            return AppResponses.Failure<bool>(Unavailable);
        }

        if (!account.RequiresAdministratorSetup)
        {
            return AppResponses.Success(true);
        }

        var role = await roles.FindByNameAsync("Tenant Administrator").ConfigureAwait(false);
        if (role?.TenantId != account.TenantId || role.TemplateKey != "iam-administrator" || role.Name is null || role.IsDeleted)
        {
            return AppResponses.Failure<bool>("The workspace administrator role is unavailable.");
        }

        if (invitation.State == AdministratorInvitationState.Claimed)
        {
            invitation.Accept(account.Id, timeProvider.GetUtcNow());
            invitation.Tenant.AdministratorUserId = account.Id;
            if (!await SaveInvitationAsync(cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<bool>(Unavailable);
            }
        }

        if (!await plane.Tenants.AnyAsync(tenant => tenant.Id == account.TenantId
            && tenant.KycApprovedAt != null && tenant.Status == TenantStatus.Active, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<bool>(Unavailable);
        }

        if (!await users.IsInRoleAsync(account, role.Name).ConfigureAwait(false)
            && !(await users.AddToRoleAsync(account, role.Name).ConfigureAwait(false)).Succeeded)
        {
            return AppResponses.Failure<bool>("Administrator access could not be granted. Sign in to resume setup.");
        }

        account.CompleteAdministratorSetup(timeProvider.GetUtcNow());
        return (await users.UpdateAsync(account).ConfigureAwait(false)).Succeeded
            ? AppResponses.Success(true)
            : AppResponses.Failure<bool>("Setup could not finish. Sign in to resume.");
    }

    public async Task<AppResponse<bool>> ChangeDeliveryAsync(Guid tenantId, bool revoke, CancellationToken cancellationToken)
    {
        var invitation = await plane.AdministratorInvitations.FindAll().AsTracking().Include(item => item.Tenant)
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
        if (invitation is null || !invitation.Tenant.IsCustomerWorkspace || (!revoke && !invitation.Tenant.CanInviteAdministrator))
        {
            return AppResponses.Failure<bool>(Unavailable);
        }

        var now = timeProvider.GetUtcNow();
        var changed = revoke ? invitation.Revoke(actor.ActorId, now) : invitation.Resend(actor.ActorId, now);
        return changed && await SaveInvitationAsync(cancellationToken).ConfigureAwait(false)
            ? AppResponses.Success(true)
            : AppResponses.Failure<bool>("The invitation is unavailable for this action. Reload and try again.");
    }

    private Task<TenantAdministratorInvitation?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = tenantScope.TenantId;
        return plane.AdministratorInvitations.FindAll().AsTracking().Include(item => item.Tenant)
            .SingleOrDefaultAsync(item => item.Id == id && item.TenantId == tenantId
                && item.Tenant.Status == TenantStatus.Active && item.Tenant.KycApprovedAt != null
                && item.Tenant.WorkspaceVersion > 0 && !item.Tenant.IsDemo, cancellationToken);
    }

    private static bool IsValid(TenantAdministratorInvitation? invitation, string token, DateTimeOffset now) =>
        invitation?.State == AdministratorInvitationState.Pending && invitation.ExpiresAt > now
        && !string.IsNullOrEmpty(token) && SingleUseCredential.Matches(invitation.TokenHash, token);

    private async Task<bool> IsEligibleAsync(AppUser account, TenantAdministratorInvitation invitation) =>
        account.IsActive && !account.IsDeleted && account.TenantId == invitation.TenantId
        && (!account.AdministratorInvitationId.HasValue || account.AdministratorInvitationId == invitation.Id)
        && !(await users.GetClaimsAsync(account).ConfigureAwait(false)).Any(claim => claim.Type == "platform_staff" && claim.Value == "true");

    private async Task<bool> IsLegacyUnclaimedAsync(AppUser account, TenantAdministratorInvitation invitation) =>
        !account.EmailConfirmed && account.Id == invitation.Tenant.AdministratorUserId
        && await IsEligibleAsync(account, invitation).ConfigureAwait(false)
        && !await users.HasPasswordAsync(account).ConfigureAwait(false)
        && (await users.GetLoginsAsync(account).ConfigureAwait(false)).Count == 0;

    private async Task<bool> HasCapacityAsync(TenantAdministratorInvitation invitation, CancellationToken cancellationToken) =>
        invitation.Tenant.MaxUsers <= 0 || await users.Users.CountAsync(account =>
            account.TenantId == invitation.TenantId && !account.IsDeleted, cancellationToken).ConfigureAwait(false) < invitation.Tenant.MaxUsers;

    private static AppUser NewAccount(TenantAdministratorInvitation invitation, DateTimeOffset now)
    {
        var account = AppUser.CreateAccount(now, invitation.TenantId, invitation.Email, invitation.FirstName,
            invitation.LastName, invitation.Email, null, invitation.CreatedBy);
        account.Id = invitation.ClaimedUserId ?? account.Id;
        account.IsActive = true;
        account.EmailConfirmed = true;
        account.CreatedAt = now;
        account.ActivatedAt = now;
        account.ActivatedBy = invitation.CreatedBy;
        return account;
    }

    private async Task<bool> ReserveAsync(TenantAdministratorInvitation invitation, string userId, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        invitation.Reserve(userId, now) && await SaveInvitationAsync(cancellationToken).ConfigureAwait(false);

    private async Task<AppResponse<bool>> ClaimAsync(TenantAdministratorInvitation invitation, CancellationToken cancellationToken)
    {
        invitation.Claim();
        return await SaveInvitationAsync(cancellationToken).ConfigureAwait(false)
            ? AppResponses.Success(true) : AppResponses.Failure<bool>(Unavailable);
    }

    private async Task<bool> SaveInvitationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await plane.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static bool IsDuplicateIdentity(DbUpdateException exception) => exception.InnerException is
        SqlException { Number: 2601 or 2627 } or PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private sealed class ExternalInvitationLinkException : Exception;
}
