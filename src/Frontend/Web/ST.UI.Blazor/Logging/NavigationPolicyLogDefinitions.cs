using Microsoft.Extensions.Logging;
namespace ST.UI.Blazor.Logging;
internal static partial class NavigationPolicyLogDefinitions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not refresh the workspace navigation policy.")]
    public static partial void RefreshFailed(ILogger logger, Exception exception);
}
