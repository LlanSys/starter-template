using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamSessionRepository : Repository<AppUserSession>, ISessionRepository
{
    private readonly IamDBContext _iamContext;

    public IamSessionRepository(IamDBContext context) : base(context)
    {
        _iamContext = context;
    }

    public async Task<List<AppUserSession>> GetActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = _iamContext.Clock.GetUtcNow();
        return await _iamContext.AppUserSessions
            .Where(s => s.AppUserId == userId && s.IsActive && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastAccessedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppUserSession?> GetTrackedByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _iamContext.AppUserSessions
            .FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AppUserSession?> GetOldestSessionByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _iamContext.AppUserSessions
            .Where(s => s.AppUserId == userId && s.IsActive)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<AppUserSession>> GetExpiredSessionsAsync(DateTimeOffset retentionLimit, CancellationToken cancellationToken = default)
    {
        return await _iamContext.AppUserSessions
            .Where(s => s.IsActive && s.IsRevoked && s.ExpiresAt <= retentionLimit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PurgeOldSessionsAsync(DateTimeOffset retentionLimit, CancellationToken cancellationToken = default)
    {
        var oldSessions = await _iamContext.AppUserSessions
            .Where(s => s.CreatedAt < retentionLimit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (!oldSessions.Any())
            return false;

        _iamContext.AppUserSessions.RemoveRange(oldSessions);
        return true;
    }
}
