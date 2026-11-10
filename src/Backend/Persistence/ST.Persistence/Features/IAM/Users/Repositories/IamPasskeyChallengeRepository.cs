using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamPasskeyChallengeRepository(IamDBContext context)
    : Repository<PasskeyChallenge>(context), IPasskeyChallengeRepository
{
    public async Task ReplaceAsync(PasskeyChallenge challenge, CancellationToken cancellationToken)
    {
        // Expiration/replacement are deliberate atomic persistence operations; no tracked account writes are committed here.
        var utcNow = context.Clock.GetUtcNow().UtcDateTime;
        var tenantId = context.CurrentTenantId;
        await context.PasskeyChallenges.Where(item => item.TenantId == tenantId && (item.Key == challenge.Key || item.ExpiresAtUtc <= utcNow))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await CreateAsync(challenge, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> ConsumeAsync(string key, DateTime utcNow, CancellationToken cancellationToken)
    {
        var challenge = await context.PasskeyChallenges.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
        if (challenge is null) return null;
        // Exactly one caller wins the delete. Failed/malformed ceremonies also consume their challenge.
        var tenantId = context.CurrentTenantId;
        var consumed = await context.PasskeyChallenges.Where(item => item.Id == challenge.Id && item.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        return consumed == 1 && challenge.ExpiresAtUtc > utcNow ? challenge.OptionsJson : null;
    }
}
