namespace ST.UI.Blazor.Logging;

internal static partial class SessionLifecycleLogDefinitions
{
    [LoggerMessage(EventId = 9120, Level = LogLevel.Warning, Message = "Session lifecycle sign-out failed during idle expiry.")]
    public static partial void LogIdleSignOutFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9121, Level = LogLevel.Warning, Message = "Session lifecycle JavaScript cleanup failed.")]
    public static partial void LogJavaScriptCleanupFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9122, Level = LogLevel.Warning, Message = "Session lifecycle check failed.")]
    public static partial void LogSessionCheckFailed(ILogger logger, Exception exception);
}
