using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Api.Logging;

internal static partial class MiddlewareLogDefinitions
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Middleware: {MiddlewareName} processed request in {ElapsedMs}ms")]
    public static partial void LogMiddlewareExecution(ILogger logger, string middlewareName, long elapsedMs);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Error, Message = "Unhandled exception in Middleware {MiddlewareName}")]
    public static partial void LogMiddlewareException(ILogger logger, string middlewareName, Exception ex);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning, Message = "Invalid session detected: {SessionId} for user: {UserId}. Reason: {Reason}")]
    public static partial void LogInvalidSession(ILogger logger, string? sessionId, string? userId, string? reason);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Debug, Message = "Error during sign out in session validation middleware")]
    public static partial void LogSignOutError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Debug, Message = "Session state is unavailable while resolving the current session id.")]
    public static partial void LogSessionStateUnavailable(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 1105, Level = LogLevel.Warning, Message = "Blocked request for user {UserId} because MFA enrollment is required. Path: {Path}")]
    public static partial void LogMfaEnrollmentRequired(ILogger logger, string userId, string path);

    [LoggerMessage(EventId = 1106, Level = LogLevel.Warning, Message = "Removed invalid property '{Key}' from schema for type {TypeName}. Consider adding [JsonIgnore] to the source property.")]
    public static partial void LogRemovedInvalidSchemaProperty(ILogger logger, string key, string typeName);
}
