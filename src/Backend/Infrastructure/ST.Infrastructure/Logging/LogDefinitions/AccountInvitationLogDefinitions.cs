using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Logging;

internal static partial class AccountInvitationLogDefinitions
{
    [LoggerMessage(EventId = 34992, Level = LogLevel.Error, Message = "Account invitation operation failed for user {UserId}")]
    internal static partial void LogFailed(ILogger logger, string userId, Exception exception);
}
