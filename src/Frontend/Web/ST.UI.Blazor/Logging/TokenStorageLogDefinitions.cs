namespace ST.UI.Blazor.Logging;

internal static partial class TokenStorageLogDefinitions
{
    [LoggerMessage(EventId = 9110, Level = LogLevel.Debug, Message = "Protected browser token storage was unavailable during read; using the circuit-scoped fallback.")]
    public static partial void LogBrowserStorageReadUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9111, Level = LogLevel.Warning, Message = "Protected browser token storage was unavailable during write; tokens are available only for the current circuit.")]
    public static partial void LogBrowserStorageWriteUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9112, Level = LogLevel.Warning, Message = "Protected browser token storage was unavailable during clear; the circuit-scoped fallback was cleared.")]
    public static partial void LogBrowserStorageClearUnavailable(ILogger logger, Exception exception);
}
