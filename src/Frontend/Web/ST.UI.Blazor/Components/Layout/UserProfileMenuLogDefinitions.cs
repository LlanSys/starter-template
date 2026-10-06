namespace ST.UI.Blazor.Components.Layout;

internal static partial class UserProfileMenuLogDefinitions
{
    [LoggerMessage(EventId = 9301, Level = LogLevel.Warning, Message = "Timed out while restoring the sidebar profile session.")]
    public static partial void LogSessionRestoreTimedOut(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9302, Level = LogLevel.Warning, Message = "Failed to restore the sidebar profile session.")]
    public static partial void LogSessionRestoreFailed(ILogger logger, Exception exception);
}
