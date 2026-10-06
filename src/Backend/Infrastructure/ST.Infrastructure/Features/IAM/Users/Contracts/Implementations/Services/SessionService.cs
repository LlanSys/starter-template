using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class SessionService(
    IIamUnitOfWork iamUnitOfWork,
    ILogger<SessionService> logger,
    IOptions<SessionSettings> sessionSettings) : ISessionService
{
    private readonly IIamUnitOfWork _unitOfWork = iamUnitOfWork;
    private readonly ILogger<SessionService> _logger = logger;
    private readonly SessionSettings _sessionSettings = sessionSettings.Value;

    public async Task<AppResponse<bool>> CheckConcurrentSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSessions = await _unitOfWork.SessionRepository
                .GetActiveSessionsByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            return activeSessions.Count >= _sessionSettings.MaxConcurrentSessions
                ? AppResponses.Failure<bool>($"Maximum {_sessionSettings.MaxConcurrentSessions} concurrent sessions allowed")
                : AppResponses.Success("Concurrent session check passed", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionConcurrentCheckError(_logger, userId, ex);
            throw;
        }
    }

    public async Task<AppResponse<Guid>> CreateSessionAsync(string userId, Guid sessionId, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check concurrent session limit
            var concurrentCheck = await CheckConcurrentSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
            if (!concurrentCheck.IsSuccess)
            {
                // If at limit, end the oldest sessions
                var endSessionsResponse = await RevokeAllUserSessionsAsync(userId, null, cancellationToken).ConfigureAwait(false);
                if (!endSessionsResponse.IsSuccess)
                {
                    ServiceLogDefinitions.LogFailedToEndOldSessions(_logger, userId, endSessionsResponse.Message ?? string.Empty);

                    return AppResponses.Failure<Guid>("Failed to end old sessions");
                }

            }

            var now = DateTimeOffset.UtcNow;
            var session = AppUserSession.Create(
                sessionId,
                userId,
                ipAddress,
                userAgent,
                now.AddMinutes(_sessionSettings.SessionTimeoutMinutes),
                userId);

            await _unitOfWork.SessionRepository.CreateAsync(session, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("Session created successfully", sessionId);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionCreateError(_logger, userId, ex);
            throw;
        }
    }

    public async Task<AppResponse<Guid>> CreateSessionAsync(string userId, Guid sessionId, string ipAddress, string userAgent, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        try
        {
            return await _unitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                // Query INSIDE transaction for consistency
                var activeSessions = await _unitOfWork.SessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

                // Check if session for THIS device exists
                var existingSessionForDevice = activeSessions.FirstOrDefault(s => s.DeviceFingerprint == deviceFingerprint);

                // Get sessions to end (exclude current device session)
                var sessionsToEnd = activeSessions.Where(s => s.DeviceFingerprint != deviceFingerprint).ToList();

                // End other device sessions
                if (sessionsToEnd.Any())
                {
                    foreach (var session in sessionsToEnd)
                    {
                        session.Revoke("New login from another device.");
                    }

                    await _unitOfWork.SessionRepository.UpdateRangeAsync(new Collection<AppUserSession>(sessionsToEnd), cancellationToken).ConfigureAwait(false);
                }

                var now = DateTimeOffset.UtcNow;

                // Update existing session OR create new one (not both)
                if (existingSessionForDevice is not null)
                {
                    existingSessionForDevice.RefreshAccess(
                        now.AddMinutes(_sessionSettings.SessionTimeoutMinutes),
                        ipAddress,
                        userAgent);

                    await _unitOfWork.SessionRepository.UpdateAsync(existingSessionForDevice, cancellationToken).ConfigureAwait(false);
                    sessionId = existingSessionForDevice.Id;
                }
                else
                {
                    var newSession = AppUserSession.Create(
                        sessionId,
                        userId,
                        ipAddress,
                        userAgent,
                        now.AddMinutes(_sessionSettings.SessionTimeoutMinutes),
                        userId,
                        deviceFingerprint);

                    await _unitOfWork.SessionRepository.CreateAsync(newSession, cancellationToken).ConfigureAwait(false);
                }

                // Commit the transaction
                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return AppResponses.Success("Session created successfully", sessionId);
            }, maxRetries, 50).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionCreateError(_logger, userId, ex);
            throw;
        }
    }

    public async Task<AppResponse<bool>> RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                return AppResponses.Failure<bool>("Invalid session id");
            }

            var session = await _unitOfWork.SessionRepository
                .GetTrackedByIdAsync(sessionGuid, cancellationToken)
                .ConfigureAwait(false);

            if (session == null)
            {
                return AppResponses.Success("Session not found", true);
            }

            session.Revoke("Session revoked by request");

            await _unitOfWork.SessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success("Session ended successfully", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionEndError(_logger, sessionId, ex);
            throw;
        }
    }

    public async Task<AppResponse<bool>> RevokeAllUserSessionsAsync(string userId, string? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSessions = await _unitOfWork.SessionRepository
                .GetActiveSessionsByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            Guid? excludeSessionGuid = null;
            if (!string.IsNullOrWhiteSpace(excludeSessionId) && Guid.TryParse(excludeSessionId, out var parsedExcludeSessionGuid))
            {
                excludeSessionGuid = parsedExcludeSessionGuid;
            }

            var sessionsToEnd = excludeSessionGuid.HasValue
                ? activeSessions.Where(s => s.Id != excludeSessionGuid.Value).ToList()
                : activeSessions;

            if (sessionsToEnd.Any())
            {
                foreach (var session in sessionsToEnd)
                {
                    session.Revoke("Concurrent session limit exceeded");
                }

                await _unitOfWork.SessionRepository.UpdateRangeAsync(new Collection<AppUserSession>(sessionsToEnd), cancellationToken).ConfigureAwait(false);

                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            return AppResponses.Success($"Ended {sessionsToEnd.Count} sessions", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogEndAllSessionsError(_logger, userId, ex);
            throw;
        }
    }

    public async Task<AppResponse<Collection<AppUserSession>>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _unitOfWork.SessionRepository
                .GetActiveSessionsByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            var sessionList = new Collection<AppUserSession>(sessions);

            return AppResponses.Success("Active sessions retrieved", sessionList);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGetActiveSessionsError(_logger, userId, ex);
            throw;
        }
    }

    public async Task<AppResponse<bool>> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var retentionLimit = now.AddDays(-30);
            var expiredSessions = await _unitOfWork.SessionRepository
                .GetExpiredSessionsAsync(now, cancellationToken)
                .ConfigureAwait(false);

            if (expiredSessions.Any())
            {
                foreach (var session in expiredSessions)
                {
                    session.Revoke("Session expired");
                }

                await _unitOfWork.SessionRepository.UpdateRangeAsync(new Collection<AppUserSession>(expiredSessions), cancellationToken).ConfigureAwait(false);

                var purgeResult = await _unitOfWork.SessionRepository
                    .PurgeOldSessionsAsync(retentionLimit, cancellationToken)
                    .ConfigureAwait(false);

                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            return AppResponses.Success($"Cleaned up {expiredSessions.Count} expired sessions", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionCleanupError(_logger, ex);
            throw;
        }
    }

    public async Task<AppResponse<bool>> IsSessionValidAsync(string sessionId, string userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return AppResponses.Failure<bool>("Invalid session id");
        }

        try
        {
            var session = await _unitOfWork.SessionRepository
                .FindByCondition(s => s.Id == sessionGuid)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (session == null || session.AppUserId != userId)
            {
                return AppResponses.Failure<bool>("Session not found");
            }

            if (!session.IsActive)
            {
                return AppResponses.Failure<bool>("Session is not active");
            }

            var now = DateTimeOffset.UtcNow;
            if (session.ExpiresAt <= now)
            {
                // Mark as expired
                session.Expire();
                await _unitOfWork.SessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

                return AppResponses.Failure<bool>("Session has expired");
            }

            // Optimize database writes: only write back if sliding expiration/access time is at least 1 minute old
            if (now - session.LastAccessedAt > TimeSpan.FromMinutes(1))
            {
                session.Touch(_sessionSettings.SlidingExpiration
                    ? now.AddMinutes(_sessionSettings.SessionTimeoutMinutes)
                    : null);

                await _unitOfWork.SessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            return AppResponses.Success("Session is valid", true);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            ServiceLogDefinitions.LogSessionConcurrencyConflict(_logger, sessionId, ex);

            // Fetch a fresh copy from DB without tracking to verify state
            var freshSession = await _unitOfWork.SessionRepository
                .FindByCondition(s => s.Id == sessionGuid)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return freshSession != null && freshSession.IsActive && freshSession.ExpiresAt > DateTimeOffset.UtcNow
                ? AppResponses.Success("Session is valid (concurrency resolved)", true)
                : AppResponses.Failure<bool>("Session has expired or is invalid");
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionValidationError(_logger, sessionId, ex);
            throw;
        }
    }

    public async Task<AppResponse<bool>> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return AppResponses.Failure<bool>("Invalid session id");
        }

        try
        {
            var session = await _unitOfWork.SessionRepository
                .FindByCondition(s => s.Id == sessionGuid)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (session == null)
            {
                return AppResponses.Failure<bool>("Session not found");
            }

            if (!session.IsActive)
            {
                return AppResponses.Failure<bool>("Session is not active");
            }

            var now = DateTimeOffset.UtcNow;
            if (session.ExpiresAt <= now)
            {
                // Mark as expired
                session.Expire();
                await _unitOfWork.SessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

                return AppResponses.Failure<bool>("Session has expired");
            }

            // Optimize database writes: only write back if sliding expiration/access time is at least 1 minute old
            if (now - session.LastAccessedAt > TimeSpan.FromMinutes(1))
            {
                session.Touch(_sessionSettings.SlidingExpiration
                    ? now.AddMinutes(_sessionSettings.SessionTimeoutMinutes)
                    : null);

                await _unitOfWork.SessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            return AppResponses.Success("Session is valid", true);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            ServiceLogDefinitions.LogSessionConcurrencyConflict(_logger, sessionId, ex);

            // Fetch a fresh copy from DB without tracking to verify state
            var freshSession = await _unitOfWork.SessionRepository
                .FindByCondition(s => s.Id == sessionGuid)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (freshSession != null && freshSession.IsActive && freshSession.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return AppResponses.Success("Session is valid (concurrency resolved)", true);
            }

            return AppResponses.Failure<bool>("Session has expired or is invalid");
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogSessionValidationError(_logger, sessionId, ex);
            throw;
        }
    }
}
