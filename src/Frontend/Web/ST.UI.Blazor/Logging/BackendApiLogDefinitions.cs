namespace ST.UI.Blazor.Logging;

internal static partial class BackendApiLogDefinitions
{
    [LoggerMessage(EventId = 9020, Level = LogLevel.Warning, Message = "Backend API request failed. Method: {Method}. Endpoint: {Endpoint}.")]
    public static partial void LogRequestFailed(ILogger logger, string method, string endpoint, Exception exception);

    [LoggerMessage(EventId = 9023, Level = LogLevel.Warning, Message = "Backend API transport failure; retrying request. Method: {Method}. Endpoint: {Endpoint}. Attempt: {Attempt}. MaxAttempts: {MaxAttempts}. DelayMs: {DelayMs}.")]
    public static partial void LogRequestRetrying(ILogger logger, string method, string endpoint, int attempt, int maxAttempts, int delayMs, Exception exception);

    [LoggerMessage(EventId = 9021, Level = LogLevel.Warning, Message = "Backend API request timed out. Method: {Method}. Endpoint: {Endpoint}.")]
    public static partial void LogRequestTimedOut(ILogger logger, string method, string endpoint, Exception exception);

    [LoggerMessage(EventId = 9022, Level = LogLevel.Warning, Message = "Backend API returned an unreadable response. StatusCode: {StatusCode}. Endpoint: {Endpoint}.")]
    public static partial void LogUnreadableResponse(ILogger logger, string statusCode, string endpoint, Exception exception);
}
