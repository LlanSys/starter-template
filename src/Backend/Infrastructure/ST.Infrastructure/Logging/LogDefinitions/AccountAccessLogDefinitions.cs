using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Logging;

internal static partial class AccountAccessLogDefinitions
{
    [LoggerMessage(EventId = 34991, Level = LogLevel.Error, Message = "Account activation failed for user {UserId}")]
    public static partial void LogActivationFailed(ILogger logger, string userId, Exception exception);
}
