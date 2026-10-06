using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
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

internal sealed class IamTempTotpSecretRepository(IamDBContext context, ILogger<IamTempTotpSecretRepository> logger) : Repository<TempTotpSecret>(context), ITempTotpSecretRepository
{
    public async Task<TempTotpSecret?> GetValidTempSecretByUserIdAsync(string userId)
    {
        return await FindByCondition(x => x.UserId == userId &&
                                         !x.IsDeleted &&
                                         x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<bool> DeleteExpiredSecretsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var expiredSecrets = await FindByCondition(x => x.ExpiresAt <= DateTimeOffset.UtcNow && !x.IsDeleted)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (var secret in expiredSecrets)
            {
                secret.MarkAsDeleted("System");
            }

            await UpdateRangeAsync(new Collection<TempTotpSecret>(expiredSecrets), cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogDeleteExpiredTempTotpSecretsError(logger, ex);
            return false;
        }
    }

    public async Task<bool> DeleteUserTempSecretsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var tempSecret = await FindByCondition(x => x.UserId == userId && !x.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (tempSecret == null) return false;

            tempSecret.MarkAsDeleted(userId);

            await UpdateAsync(tempSecret).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogDeleteUserTempTotpSecretsError(logger, userId, ex);
            return false;
        }
    }
}
