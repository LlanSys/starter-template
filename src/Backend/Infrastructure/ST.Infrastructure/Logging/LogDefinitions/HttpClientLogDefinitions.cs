using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging;

internal static partial class HttpClientLogDefinitions
{
    [LoggerMessage(EventId = 3200, Level = LogLevel.Error, Message = "External API call to {Uri} failed. Method: {Method}")]
    public static partial void LogExternalApiError(ILogger logger, string method, string uri, Exception ex);

    [LoggerMessage(EventId = 3201, Level = LogLevel.Warning, Message = "External API {Uri} returned {StatusCode}. Method: {Method}")]
    public static partial void LogExternalApiWarning(ILogger logger, string method, string uri, int statusCode);

    [LoggerMessage(EventId = 3202, Level = LogLevel.Debug, Message = "Request: {Method} {Path} with body: {Body}")]
    public static partial void LogRequest(ILogger logger, string method, string path, string body);

    [LoggerMessage(EventId = 3203, Level = LogLevel.Warning, Message = "HttpClient BaseAddress was null. Defaulting to {BaseUrl}")]
    public static partial void LogBaseAddressFallback(ILogger logger, string baseUrl);

    [LoggerMessage(EventId = 3204, Level = LogLevel.Information, Message = "Auth token expired. Expiry: {Expiry}")]
    public static partial void LogTokenExpired(ILogger logger, DateTime expiry);

    [LoggerMessage(EventId = 3205, Level = LogLevel.Debug, Message = "ApiService initialized for {BaseAddress}")]
    public static partial void LogApiServiceDebug(ILogger logger, Uri? baseAddress);

    [LoggerMessage(EventId = 3206, Level = LogLevel.Debug, Message = "External API error content was not valid JSON.")]
    public static partial void LogNonJsonErrorContent(ILogger logger, Exception ex);
}
