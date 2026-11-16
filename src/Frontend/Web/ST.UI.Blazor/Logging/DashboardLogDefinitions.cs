namespace ST.UI.Blazor.Logging;

internal static partial class DashboardLogDefinitions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Dashboard snapshot could not be loaded.")]
    internal static partial void LoadFailed(ILogger logger, Exception exception);
}
