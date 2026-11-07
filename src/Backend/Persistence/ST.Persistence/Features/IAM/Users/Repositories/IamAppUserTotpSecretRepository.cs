using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ST.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamAppUserTotpSecretRepository(IamDBContext context, ILogger<IamAppUserTotpSecretRepository> logger) : Repository<AppUserTotpSecret>(context), IAppUserTotpSecretRepository
{
    public async Task<AppUserTotpSecret?> GetByUserIdAsync(string userId)
    {
        return await FindByCondition(x => x.AppUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync().ConfigureAwait(false);
    }

    public async Task<AppUserTotpSecret?> GetActiveSecretByUserIdAsync(string userId)
    {
        var now = context.Clock.GetUtcNow();
        return await FindByCondition(x =>
                x.AppUserId == userId &&
                x.IsActive &&
                (x.ExpiresAt == null || x.ExpiresAt > now))
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeactivateUserSecretsAsync(string userId)
    {
        try
        {
            var secrets = await FindByCondition(x => x.AppUserId == userId && x.IsActive)
                .ToListAsync().ConfigureAwait(false);

            var now = context.Clock.GetUtcNow();
            foreach (var secret in secrets)
            {
                secret.Deactivate(now);
            }

            await UpdateRangeAsync(new Collection<AppUserTotpSecret>(secrets)).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogDeactivateUserTotpSecretsError(logger, userId, ex);
            return false;
        }
    }
}
