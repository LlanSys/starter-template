using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface ISessionService
{
    Task<AppResponse<bool>> CheckConcurrentSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task<AppResponse<Guid>> CreateSessionAsync(string userId, Guid sessionId, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task<AppResponse<Guid>> CreateSessionAsync(string userId, Guid sessionId, string ipAddress, string userAgent, string deviceFingerprint, CancellationToken cancellationToken = default);
    Task<AppResponse<Collection<AppUserSession>>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> IsSessionValidAsync(string sessionId, string userId, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> RevokeAllUserSessionsAsync(string userId, string? excludeSessionId = null, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
