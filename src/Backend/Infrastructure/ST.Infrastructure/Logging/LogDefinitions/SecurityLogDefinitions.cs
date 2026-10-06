using ST.Application.Contracts.Interfaces.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging;

internal static partial class SecurityLogDefinitions
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Warning, Message = "Security Event: {EventType} for User {UserId}. Details: {Details}")]
    public static partial void LogSecurityEvent(ILogger logger, string eventType, string userId, string details);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Error, Message = "An error occurred while changing password.")]
    public static partial void LogPasswordChangeError(ILogger logger, Exception ex);
}
