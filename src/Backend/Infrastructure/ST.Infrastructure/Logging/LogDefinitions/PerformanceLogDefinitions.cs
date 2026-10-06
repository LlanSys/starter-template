using ST.Application.Contracts.Interfaces.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging;

internal static partial class PerformanceLogDefinitions
{
    [LoggerMessage(EventId = 3100, Level = LogLevel.Information, Message = "Performance Metric: {Operation} took {ElapsedMs}ms")]
    public static partial void LogPerformanceMetric(ILogger logger, string operation, long elapsedMs);
}
