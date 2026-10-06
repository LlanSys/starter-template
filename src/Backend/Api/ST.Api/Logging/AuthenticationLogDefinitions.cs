namespace ST.Api.Logging;

internal static partial class AuthenticationLogDefinitions
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "User {UserId} logged in successfully via {Scheme}")]
    public static partial void LogLoginSuccess(ILogger logger, string userId, string scheme);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Failed login attempt for User {UserId}: {Reason}")]
    public static partial void LogLoginFailure(ILogger logger, string userId, string reason);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Token refreshed for User {UserId}")]
    public static partial void LogTokenRefreshed(ILogger logger, string userId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "JWT authentication failed: {Reason}")]
    public static partial void LogJwtAuthenticationFailed(ILogger logger, string reason, Exception ex);
}
