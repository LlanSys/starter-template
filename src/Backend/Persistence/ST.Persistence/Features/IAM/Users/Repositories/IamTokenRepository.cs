using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamTokenRepository : Repository<RefreshToken>, ITokenRepository
{
    private readonly IamDBContext _iamContext;

    public IamTokenRepository(IamDBContext context) : base(context)
    {
        _iamContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _iamContext.RefreshTokens.AddAsync(refreshToken).ConfigureAwait(false);
    }

    public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await FindByCondition(token =>
            token.AppUserId == userId &&
            !token.RevokedAt.HasValue &&
            now < token.ExpiresAt &&
            !token.UsedAt.HasValue)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetByTokenAndUserAsync(string token, string userId)
    {
        return await _iamContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.Token == token && t.AppUserId == userId).ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetTokenAsync(string token)
    {
        return await FindByCondition(t => t.Token == token).AsNoTracking().FirstOrDefaultAsync().ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, string userId)
    {
        return await _iamContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token && rt.AppUserId == userId).ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _iamContext.RefreshTokens
            .Include(rt => rt.AppUser)
            .FirstOrDefaultAsync(rt => rt.Token == token).ConfigureAwait(false);
    }

    public async Task<List<RefreshToken>> GetUserTokensAsync(string userId, int limit = 10)
    {
        return await _iamContext.RefreshTokens
            .Where(rt => rt.AppUserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Take(limit)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<List<RefreshToken>> GetExpiredTokensAsync(int daysOld = 30)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysOld);
        return await _iamContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTimeOffset.UtcNow && rt.CreatedAt < cutoffDate)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        _iamContext.RefreshTokens.Update(refreshToken);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task RevokeTokensAsync(List<RefreshToken> tokens, string reason, string? revokedByIp = null)
    {
        if (!tokens.Any()) return;
        revokedByIp ??= "Unknown";

        foreach (var token in tokens)
        {
            token.Revoke(reason, revokedByIp);
            await UpdateAsync(token).ConfigureAwait(false);
        }
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string reason, string? revokedByIp = null)
    {
        refreshToken.Revoke(reason, revokedByIp);
        await UpdateRefreshTokenAsync(refreshToken).ConfigureAwait(false);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string reason)
    {
        refreshToken.Revoke(reason);
        _iamContext.RefreshTokens.Update(refreshToken);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task RevokeAllUserTokensAsync(string userId, string reason, string? revokedByIp = null)
    {
        var activeTokens = await GetActiveTokensByUserIdAsync(userId).ConfigureAwait(false);
        if (!activeTokens.Any()) return;

        foreach (var token in activeTokens)
        {
            token.Revoke(reason, revokedByIp);
        }

        _iamContext.UpdateRange(activeTokens);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<bool> IsTokenActiveAsync(string token)
    {
        var now = DateTimeOffset.UtcNow;
        return await FindByCondition(t =>
                t.Token == token &&
                !t.RevokedAt.HasValue &&
                now < t.ExpiresAt &&
                !t.UsedAt.HasValue)
            .AsNoTracking()
            .AnyAsync().ConfigureAwait(false);
    }

    public async Task MarkTokenAsUsedAsync(RefreshToken refreshToken)
    {
        refreshToken.MarkAsUsed();
        _iamContext.RefreshTokens.Update(refreshToken);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task CleanupExpiredTokensAsync(string? userId = null)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
        var now = DateTimeOffset.UtcNow;

        var query = _iamContext.RefreshTokens
            .Where(rt =>
                rt.ExpiresAt < now ||
                rt.CreatedAt < cutoffDate ||
                (rt.RevokedAt.HasValue && rt.RevokedAt < cutoffDate));

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(t => t.AppUserId == userId);
        }

        var expiredTokens = await query.ToListAsync().ConfigureAwait(false);

        if (expiredTokens.Any())
        {
            _iamContext.RefreshTokens.RemoveRange(expiredTokens);
        }
    }

    public async Task PerformMaintenanceAsync()
    {
        await CleanupExpiredTokensAsync().ConfigureAwait(false);
    }
}
