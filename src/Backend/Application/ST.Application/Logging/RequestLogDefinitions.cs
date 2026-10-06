using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Utilities;

internal static partial class RequestLogDefinitions
{
    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Started handling {RequestName}")]
    public static partial void LogRequestStarted(ILogger logger, string requestName);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Warning, Message = "Slow request detected: {RequestName} took {ElapsedMs}ms")]
    public static partial void LogSlowRequest(ILogger logger, string requestName, long elapsedMs);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMs}ms")]
    public static partial void LogRequestCompleted(ILogger logger, string requestName, long elapsedMs);
}
