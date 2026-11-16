namespace ST.UI.Blazor.Logging;

internal static partial class SignInLogDefinitions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-in initialization could not be completed.")]
    internal static partial void InitializationFailed(ILogger logger, Exception exception);
    [LoggerMessage(Level = LogLevel.Warning, Message = "External sign-in options could not be loaded.")]
    internal static partial void ProvidersUnavailable(ILogger logger, Exception exception);
}
