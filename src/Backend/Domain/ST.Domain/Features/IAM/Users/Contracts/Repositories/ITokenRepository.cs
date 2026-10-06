using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Contracts.Repositories;

public interface ITokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetTokenAsync(string token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, string userId);
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId);
    Task<RefreshToken?> GetByTokenAndUserAsync(string token, string userId);
    Task<List<RefreshToken>> GetUserTokensAsync(string userId, int limit = 10);
    Task<List<RefreshToken>> GetExpiredTokensAsync(int daysOld = 30);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task MarkTokenAsUsedAsync(RefreshToken refreshToken);
    Task<bool> IsTokenActiveAsync(string token);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string reason);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string reason, string? revokedByIp = null);
    Task RevokeTokensAsync(List<RefreshToken> tokens, string reason, string? revokedByIp = null);
    Task RevokeAllUserTokensAsync(string userId, string reason, string? revokedByIp = null);
    Task CleanupExpiredTokensAsync(string? userId = null);
    Task PerformMaintenanceAsync();
}
