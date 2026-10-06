namespace ST.UI.Blazor.Logging;

internal static partial class AuthSessionLogDefinitions
{
    [LoggerMessage(EventId = 9101, Level = LogLevel.Warning, Message = "Auth session initialization failed.")]
    public static partial void LogSessionInitializationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9102, Level = LogLevel.Warning, Message = "Auth session storage cleanup failed.")]
    public static partial void LogSessionStorageClearFailed(ILogger logger, Exception exception);
}
