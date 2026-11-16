using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Staff;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.PlatformStaff.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Infrastructure.Features.ControlPlane.Staff;

internal sealed partial class PlatformStaffService(
    IamDBContext db,
    IIamUnitOfWork unit,
    UserManager<AppUser> users,
    ICurrentTenantProvider tenant,
    ICurrentActorProvider actor,
    IHttpContextAccessor http,
    ISender sender,
    TimeProvider clock,
    ILogger<PlatformStaffService> logger
) : IPlatformStaffService
{
    private IQueryable<AppUser> Accounts => db.Users.IgnoreQueryFilters().Where(u => u.TenantId == Tenant.PlatformTenantId && !u.IsDeleted && u.Kind == AccountKind.Human);

    public async Task<AppResponse<PlatformStaffPageResponse>> ListAsync(string? search, int page, CancellationToken ct)
    {
        if (!await AuthorizedAsync(PlatformPermissions.ViewPlatformStaff, ct).ConfigureAwait(false))
        {
            return Denied<PlatformStaffPageResponse>();
        }

        if (page < 1 || page > 100000)
        {
            return AppResponses.Failure<PlatformStaffPageResponse>("Select a valid page.");
        }

        var query = Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.UserName!.Contains(term) || u.Email!.Contains(term) || u.FirstName.Contains(term) || u.LastName.Contains(term));
        }

        var count = await query.CountAsync(ct).ConfigureAwait(false);
        var accounts = await query.OrderBy(u => u.UserName).Skip((page - 1) * 25).Take(25).ToArrayAsync(ct).ConfigureAwait(false);
        var ids = accounts.Select(u => u.Id).ToArray();
        var claims = await db.UserClaims.AsNoTracking().Where(c => ids.Contains(c.UserId)).ToArrayAsync(ct).ConfigureAwait(false);
        var items = accounts.Select(u => Describe(u, claims.Where(c => c.UserId == u.Id))).ToArray();
        return AppResponses.Success(new PlatformStaffPageResponse(items, count, page, 25));
    }

    public async Task<AppResponse<IReadOnlyList<PlatformStaffAuditResponse>>> HistoryAsync(
        string userId,
        CancellationToken ct
    )
    {
        if (!await AuthorizedAsync(PlatformPermissions.ViewPlatformStaff, ct).ConfigureAwait(false))
        {
            return Denied<IReadOnlyList<PlatformStaffAuditResponse>>();
        }

        if (!await Accounts.AnyAsync(u => u.Id == userId, ct).ConfigureAwait(false))
        {
            return AppResponses.NotFound<IReadOnlyList<PlatformStaffAuditResponse>>("Account not found.");
        }

        var records = await db.PlatformStaffChanges.AsNoTracking().Where(e => e.UserId == userId).OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).Take(100).ToArrayAsync(ct).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<PlatformStaffAuditResponse>>(records.Select(e => new PlatformStaffAuditResponse(
            e.Id,
            e.UserId,
            e.ActorId,
            e.Action,
            e.Reason,
            e.PreviousStaff,
            e.CurrentStaff,
            e.PreviousActive,
            e.CurrentActive,
            JsonSerializer.Deserialize<string[]>(e.PreviousPermissionsJson)!,
            JsonSerializer.Deserialize<string[]>(e.CurrentPermissionsJson)!,
            e.OccurredAt
        )).ToArray());
    }

    public Task<AppResponse<PlatformStaffResponse>> CreateAsync(
        CreatePlatformStaffRequest request,
        CancellationToken ct
    )
    {
        return MutateAsync(async () =>
        {
            var keys = Normalize(request.PermissionKeys);

            if (!await AuthorizedAsync(PlatformPermissions.ManagePlatformStaff, ct).ConfigureAwait(false) || !await CanDelegateAsync(keys, ct).ConfigureAwait(false))
            {
                return Denied<PlatformStaffResponse>();
            }

            var now = clock.GetUtcNow();
            var user = AppUser.CreateAccount(
                now,
                Tenant.PlatformTenantId,
                request.Username,
                request.FirstName,
                request.LastName,
                request.Email,
                null,
                actor.ActorId
            );

            user.IsActive = true;
            Ensure(await users.CreateAsync(user).ConfigureAwait(false));
            Ensure(await users.AddClaimsAsync(user, keys.Select(k => new Claim("permission", k)).Prepend(new Claim(PlatformAccess.StaffClaim, "true"))).ConfigureAwait(false));
            await AuditAsync(user.Id, "AccountAdded", request.Reason, false, true, false, true, [], keys, now, ct).ConfigureAwait(false);
            await unit.CompleteAsync(ct).ConfigureAwait(false);
            return AppResponses.Success("Account saved. Send its invitation to begin setup.", await DescribeAsync(user, ct).ConfigureAwait(false));
        }, ct);
    }

    public Task<AppResponse<PlatformStaffResponse>> UpdateAsync(
        string userId,
        UpdatePlatformStaffRequest request,
        CancellationToken ct
    )
    {
        return MutateAsync(async () =>
        {
            if (!await AuthorizedAsync(PlatformPermissions.ManagePlatformStaff, ct).ConfigureAwait(false))
            {
                return Denied<PlatformStaffResponse>();
            }

            var user = await Accounts.AsTracking().SingleOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.NotFound<PlatformStaffResponse>("Account not found.");
            }

            if (user.ConcurrencyStamp != request.Revision)
            {
                return AppResponses.Failure<PlatformStaffResponse>("This account changed. Refresh before saving.");
            }

            var claims = await users.GetClaimsAsync(user).ConfigureAwait(false);
            var oldKeys = Normalize(claims.Where(c => c.Type == "permission" && PlatformPermissions.IsDefined(c.Value)).Select(c => c.Value));
            var keys = request.IsStaff ? Normalize(request.PermissionKeys) : [];
            var wasStaff = claims.Any(c => c.Type == PlatformAccess.StaffClaim && c.Value == "true");

            if (!await CanDelegateAsync(oldKeys.Union(keys, StringComparer.OrdinalIgnoreCase), ct).ConfigureAwait(false))
            {
                return Denied<PlatformStaffResponse>();
            }

            if (wasStaff && user.IsActive && user.EmailConfirmed && user.TwoFactorEnabled && oldKeys.Contains(PlatformPermissions.ManagePlatformStaff) && (!request.IsStaff || !request.IsActive || !keys.Contains(PlatformPermissions.ManagePlatformStaff)) && !await OtherManagerAsync(user.Id, ct).ConfigureAwait(false))
            {
                return AppResponses.Failure<PlatformStaffResponse>("Keep at least one active, verified platform staff administrator with MFA and staff-management permission.");
            }

            var now = clock.GetUtcNow();
            var wasActive = user.IsActive;
            Ensure(await users.RemoveClaimsAsync(user, claims.Where(c => c.Type == PlatformAccess.StaffClaim || (c.Type == "permission" && PlatformPermissions.IsDefined(c.Value)))).ConfigureAwait(false));
            var newClaims = keys.Select(k => new Claim("permission", k)).ToList();

            if (request.IsStaff)
            {
                newClaims.Add(new Claim(PlatformAccess.StaffClaim, "true"));
            }

            if (newClaims.Count > 0)
            {
                Ensure(await users.AddClaimsAsync(user, newClaims).ConfigureAwait(false));
            }

            if (request.IsActive && !wasActive)
            {
                user.GrantAccess(now, actor.ActorId);
            }

            if (!request.IsActive && wasActive)
            {
                user.RevokeAccess(now, actor.ActorId, request.Reason.Trim());
            }

            user.UpdatedAt = now;
            user.UpdatedBy = actor.ActorId;
            // Explicit audit below owns this security operation; do not publish generic account events twice.
            user.ClearDomainEvents();
            Ensure(await users.UpdateSecurityStampAsync(user).ConfigureAwait(false));
            await AccountAccessCleanup.StageAsync(
                unit,
                user.Id,
                actor.ActorId,
                "Platform staff access changed",
                now,
                ct
            ).ConfigureAwait(false);

            await AuditAsync(
                user.Id,
                "AccessChanged",
                request.Reason,
                wasStaff,
                request.IsStaff,
                wasActive,
                request.IsActive,
                oldKeys,
                keys,
                now,
                ct
            ).ConfigureAwait(false);

            await unit.CompleteAsync(ct).ConfigureAwait(false);
            return AppResponses.Success("Access saved. The account must sign in again.", await DescribeAsync(user, ct).ConfigureAwait(false));
        }, ct);
    }

    public async Task<AppResponse<bool>> InviteAsync(
        string userId,
        PlatformStaffInvitationRequest request,
        CancellationToken ct
    )
    {
        var prepared = await MutateAsync(async () =>
        {
            if (!await AuthorizedAsync(PlatformPermissions.ManagePlatformStaff, ct).ConfigureAwait(false))
            {
                return Denied<bool>();
            }

            var user = await Accounts.AsTracking().SingleOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.NotFound<bool>("Account not found.");
            }

            if (user.ConcurrencyStamp != request.Revision)
            {
                return AppResponses.Failure<bool>("This account changed. Refresh before sending its invitation.");
            }

            var claims = await users.GetClaimsAsync(user).ConfigureAwait(false);
            var keys = Normalize(claims.Where(c => c.Type == "permission" && PlatformPermissions.IsDefined(c.Value)).Select(c => c.Value));

            if (!claims.Any(c => c.Type == PlatformAccess.StaffClaim && c.Value == "true") || !user.IsActive || user.EmailConfirmed)
            {
                return AppResponses.Failure<bool>("Invitations are available only for active platform staff awaiting email verification.");
            }

            if (!await CanDelegateAsync(keys, ct).ConfigureAwait(false))
            {
                return Denied<bool>();
            }

            Ensure(await users.UpdateSecurityStampAsync(user).ConfigureAwait(false));
            await AuditAsync(
                user.Id,
                "InvitationRequested",
                request.Reason,
                true,
                true,
                true,
                true,
                keys,
                keys,
                clock.GetUtcNow(),
                ct
            ).ConfigureAwait(false);

            await unit.CompleteAsync(ct).ConfigureAwait(false);
            return AppResponses.Success(true);
        }, ct).ConfigureAwait(false);

        if (!prepared.IsSuccess)
        {
            return prepared;
        }

        // Delivery follows the durable account/audit commit. Retry rotates the previous invitation token.
        return await sender.Send(new InviteAdminUserCommand(userId, PlatformStaffWorkflow: true), ct).ConfigureAwait(false);
    }

    private async Task<AppResponse<T>> MutateAsync<T>(Func<Task<AppResponse<T>>> change, CancellationToken ct)
    {
        try
        {
            return await unit.ExecuteInTransactionAsync(async () =>
            {
                await LockAsync(ct).ConfigureAwait(false);
                return await change().ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (StaffChangeRejectedException ex)
        {
            ChangeRejected(logger, ex);
            return AppResponses.Failure<T>("The account could not be saved. Check for duplicate details or concurrent changes, then refresh and try again.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            ChangeRejected(logger, ex);
            return AppResponses.Failure<T>("This account changed. Refresh before saving.");
        }
    }

    private async Task LockAsync(CancellationToken ct)
    {
        if (db.Database.IsSqlServer())
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource = N'ST:IAM:PlatformStaffAuthority', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000; IF @r < 0 THROW 51000, 'Authority lock unavailable', 1;", ct).ConfigureAwait(false);
        }
        else if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731043, 1)", ct).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException("Platform staff administration requires a supported database provider.");
        }
    }

    private async Task<bool> AuthorizedAsync(string permission, CancellationToken ct)
    {
        if (tenant.TenantId != Tenant.PlatformTenantId || !PlatformAccess.HasMfa(http.HttpContext?.User ?? new ClaimsPrincipal()))
        {
            return false;
        }

        var id = actor.ActorId;
        return await Accounts.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive && u.EmailConfirmed && u.TwoFactorEnabled && db.UserClaims.Any(c => c.UserId == u.Id && c.ClaimType == PlatformAccess.StaffClaim && c.ClaimValue == "true") && db.UserClaims.Any(c => c.UserId == u.Id && c.ClaimType == "permission" && c.ClaimValue == permission), ct).ConfigureAwait(false);
    }

    private async Task<bool> CanDelegateAsync(IEnumerable<string> requested, CancellationToken ct)
    {
        var id = actor.ActorId;
        var held = await db.UserClaims.AsNoTracking().Where(c => c.UserId == id && c.ClaimType == "permission").Select(c => c.ClaimValue!).ToArrayAsync(ct).ConfigureAwait(false);
        return requested.All(k => PlatformPermissions.IsDefined(k) && held.Contains(k, StringComparer.OrdinalIgnoreCase));
    }

    private Task<bool> OtherManagerAsync(string excluded, CancellationToken ct)
    {
        return Accounts.AsNoTracking().AnyAsync(u => u.Id != excluded && u.IsActive && u.EmailConfirmed && u.TwoFactorEnabled && db.UserClaims.Any(c => c.UserId == u.Id && c.ClaimType == PlatformAccess.StaffClaim && c.ClaimValue == "true") && db.UserClaims.Any(c => c.UserId == u.Id && c.ClaimType == "permission" && c.ClaimValue == PlatformPermissions.ManagePlatformStaff), ct);
    }

    private async Task<PlatformStaffResponse> DescribeAsync(AppUser user, CancellationToken ct)
    {
        var claims = await db.UserClaims.AsNoTracking().Where(c => c.UserId == user.Id).ToArrayAsync(ct).ConfigureAwait(false);
        return Describe(user, claims);
    }

    private static PlatformStaffResponse Describe(AppUser u, IEnumerable<IdentityUserClaim<string>> claims)
    {
        var staff = claims.Any(c => c.ClaimType == PlatformAccess.StaffClaim && c.ClaimValue == "true");
        return new PlatformStaffResponse(
            u.Id,
            u.UserName!,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.Email!,
            staff,
            u.IsActive,
            u.EmailConfirmed,
            u.TwoFactorEnabled,
            !u.IsActive ? "Inactive" : !staff ? "Not Staff" : !u.EmailConfirmed ? "Invitation Pending" : !u.TwoFactorEnabled ? "MFA Required" : "Active",
            u.ConcurrencyStamp!,
            Normalize(claims.Where(c => c.ClaimType == "permission" && PlatformPermissions.IsDefined(c.ClaimValue!)).Select(c => c.ClaimValue!))
        );
    }

    private Task AuditAsync(
        string userId,
        string action,
        string reason,
        bool oldStaff,
        bool staff,
        bool oldActive,
        bool active,
        string[] oldKeys,
        string[] keys,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        return unit.PlatformStaffChanges.CreateAsync(new PlatformStaffChange(
            Tenant.PlatformTenantId,
            userId,
            actor.ActorId,
            action,
            reason.Trim(),
            oldStaff,
            staff,
            oldActive,
            active,
            JsonSerializer.Serialize(oldKeys),
            JsonSerializer.Serialize(keys),
            now
        ), ct);
    }

    private static string[] Normalize(IEnumerable<string> keys)
    {
        return keys.Select(key => key.ToLowerInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
    }

    private static AppResponse<T> Denied<T>()
    {
        return AppResponses.Failure<T>(AppError.Forbidden("You cannot administer these platform staff permissions."));
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new StaffChangeRejectedException();
        }
    }

    private sealed class StaffChangeRejectedException : Exception
    {
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A platform staff change was rejected.")]
    private static partial void ChangeRejected(ILogger logger, Exception exception);
}
