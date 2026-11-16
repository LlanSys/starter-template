using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Persistence.Features.IAM.Users.Stores;

public sealed class TenantUserStore(
    IamDBContext context,
    ITenantAccountLimitProvider limits,
    IdentityErrorDescriber errors) : UserStore<AppUser, AppRole, IamDBContext, string>(context, errors)
{
    public override Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        return MutateAsync(user, true, cancellationToken);
    }

    public override Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        return MutateAsync(user, false, cancellationToken);
    }

    private async Task<IdentityResult> MutateAsync(AppUser user, bool creating, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.TenantId == Guid.Empty || user.TenantId != context.CurrentTenantId)
        {
            return Rejected("TenantUnavailable", "The account workspace could not be validated.");
        }

        var persisted = creating ? null : await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(account => account.Id == user.Id)
            .Select(account => new { account.TenantId, account.IsDeleted, account.Kind,
                account.NormalizedUserName, account.NormalizedEmail })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (!creating && (persisted is null || persisted.TenantId != user.TenantId))
        {
            return Rejected("TenantUnavailable", "The account workspace could not be validated.");
        }

        var restoringSeat = !creating && !user.IsDeleted && user.Kind == AccountKind.Human
            && (persisted!.IsDeleted || persisted.Kind != AccountKind.Human);

        var identityChanged = creating || !string.Equals(persisted!.NormalizedUserName, user.NormalizedUserName, StringComparison.Ordinal)
            || !string.Equals(persisted.NormalizedEmail, user.NormalizedEmail, StringComparison.Ordinal);

        if (!identityChanged && !restoringSeat)
        {
            return await base.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }

        async Task<IdentityResult> WriteAsync()
        {
            IDbContextTransaction? owned = null;

            try
            {
                if (context.Database.CurrentTransaction is null)
                {
                    owned = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                }
                await LockTenantAsync(user.TenantId, cancellationToken).ConfigureAwait(false);

                if (await context.Users.IgnoreQueryFilters().AsNoTracking().AnyAsync(account => account.TenantId == user.TenantId
                    && account.Id != user.Id && (account.NormalizedUserName == user.NormalizedUserName
                        || (user.NormalizedEmail != null && account.NormalizedEmail == user.NormalizedEmail)), cancellationToken).ConfigureAwait(false))
                {
                    return Rejected("DuplicateTenantIdentity", "An account with these details already exists in this workspace.");
                }

                if ((creating || restoringSeat) && !user.IsDeleted && user.Kind == AccountKind.Human)
                {
                    var limit = await limits.GetLimitAsync(user.TenantId, cancellationToken).ConfigureAwait(false);

                    if (!limit.HasValue)
                    {
                        return Rejected("TenantUnavailable", "The account workspace could not be validated.");
                    }

                    if (limit.Value > 0 && await context.Users.IgnoreQueryFilters()
                        .CountAsync(account => account.TenantId == user.TenantId && !account.IsDeleted
                            && account.Kind == AccountKind.Human && account.Id != user.Id, cancellationToken).ConfigureAwait(false) >= limit.Value)
                    {
                        return Rejected("TenantAccountLimitReached", "This workspace has reached its user limit.");
                    }
                }

                var result = creating
                    ? await base.CreateAsync(user, cancellationToken).ConfigureAwait(false)
                    : await base.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

                if (result.Succeeded && owned is not null)
                {
                    await owned.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                return result;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                // Translate expected uniqueness races without leaking provider details.
                return Rejected("DuplicateTenantIdentity", "An account with these details already exists in this workspace.");
            }
            finally
            {
                if (owned is not null)
                {
                    await owned.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        if (context.Database.CurrentTransaction is not null)
        {
            return await WriteAsync().ConfigureAwait(false);
        }
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(WriteAsync).ConfigureAwait(false);
    }

    private async Task LockTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var resource = $"ST:IAM:AccountAdmission:{tenantId:N}";

        if (context.Database.IsSqlServer())
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode=N'Exclusive',
                    @LockOwner=N'Transaction', @LockTimeout=15000;
                IF @result < 0 THROW 51000, 'Account admission lock unavailable.', 1;
                """, cancellationToken).ConfigureAwait(false);
        }
        else if (context.Database.IsNpgsql())
        {
            var key = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken).ConfigureAwait(false);
        }
        else if (!string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Account admission requires a supported transactional database provider.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: "23505" }
            or SqlException { Number: 2601 or 2627 };
    }

    private static IdentityResult Rejected(string code, string message)
    {
        return IdentityResult.Failed(new IdentityError { Code = code, Description = message });
    }
}
