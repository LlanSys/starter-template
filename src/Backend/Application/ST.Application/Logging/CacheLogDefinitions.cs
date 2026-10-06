using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Utilities;

internal static partial class CacheLogDefinitions
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug, Message = "[Cache] HIT {Key}")]
    public static partial void LogCacheHit(ILogger logger, string key);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "[Cache] MISS {Key}")]
    public static partial void LogCacheMiss(ILogger logger, string key);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "[Cache] BUMPED {SentinelKey} -> {Version}")]
    public static partial void LogCacheBumped(ILogger logger, string sentinelKey, string version);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug, Message = "[Cache] SET {Key} (TTL {Ttl})")]
    public static partial void LogCacheSet(ILogger logger, string key, TimeSpan ttl);
}
