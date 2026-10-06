using ST.Domain.Features.IAM.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal static class AccountAccessCleanup
{
    // Stage all writes on the same IAM context; the caller owns the transaction and commit.
    public static async Task StageAsync(IIamUnitOfWork unitOfWork, string userId, string actorId,
        string reason, CancellationToken cancellationToken)
    {
        var sessions = await unitOfWork.SessionRepository
            .FindByCondition(session => session.AppUserId == userId && session.IsActive)
            .AsTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var session in sessions)
        {
            session.Revoke(reason);
            session.UpdatedBy = actorId;
        }
        var tokens = await unitOfWork.TokenRepository
            .FindByCondition(token => token.AppUserId == userId && token.RevokedAt == null)
            .AsTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var token in tokens)
        {
            token.Revoke(reason);
            token.UpdatedBy = actorId;
        }
    }
}
