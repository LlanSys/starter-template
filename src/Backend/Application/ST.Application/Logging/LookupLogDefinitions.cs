using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Utilities;

internal static partial class LookupLogDefinitions
{
    [LoggerMessage(EventId = 2200, Level = LogLevel.Warning, Message = "Invalid lookup type: {LookupType}")]
    public static partial void LogInvalidLookupType(ILogger logger, string lookupType, Exception ex);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Successfully retrieved {Count} lookups for type: {LookupType}")]
    public static partial void LogLookupsRetrieved(ILogger logger, int count, string lookupType);
}
