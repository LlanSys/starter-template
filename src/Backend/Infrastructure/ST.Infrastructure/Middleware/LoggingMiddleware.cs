using ST.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ST.Infrastructure.Middleware;

/// <summary>
/// Adds a <c>CorrelationId</c> response header and optionally logs request bodies at Debug level.
/// </summary>
/// <remarks>
/// Request body logging is gated behind <c>LogLevel.Debug</c> — it is never active in
/// Production where the minimum level is Information. Do not enable Debug in Production.
/// <para>
/// <b>Why not <c>partial</c>?</b> There is no generated counterpart for this class.
/// The <c>partial</c> modifier was removed — it was misleading and served no purpose.
/// </para>
/// </remarks>
internal sealed class LoggingMiddleware(RequestDelegate next,ILogger<LoggingMiddleware> logger)
{
    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "accessToken",
        "apiKey",
        "authorization",
        "code",
        "consumerKey",
        "consumerSecret",
        "jwt",
        "passKey",
        "password",
        "refreshToken",
        "secret",
        "secretKey",
        "token",
        "webhookSigningSecret"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Correlation ID is already set by CorrelationIdEnricher via Serilog.
        // We just echo it in the response header for client-side correlation.
        context.Response.Headers.TryAdd("X-Correlation-ID", context.TraceIdentifier);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            // EnableBuffering allows the body to be read multiple times.
            // Without it, reading here would consume the stream and the
            // downstream controller would receive an empty body.
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                leaveOpen: true);              // must leave open — downstream still needs it

            string body = string.Empty;
            try
            {
                body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException)
            {
                body = "[request-body-read-failed-client-disconnected]";
            }

            // Reset position so the controller reads from the beginning
            context.Request.Body.Position = 0;

            var redactedBody = RedactRequestBody(body);
            HttpClientLogDefinitions.LogRequest(
                logger,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                redactedBody);
        }

        await next(context).ConfigureAwait(false);
    }

    private static string RedactRequestBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            var json = JsonNode.Parse(body);
            if (json is null)
            {
                return "[empty-json-body]";
            }

            RedactJsonNode(json);
            return json.ToJsonString();
        }
        catch (JsonException)
        {
            return "[non-json-request-body-redacted]";
        }
    }

    private static void RedactJsonNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (SensitiveFieldNames.Contains(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                    continue;
                }

                if (property.Value is not null)
                {
                    RedactJsonNode(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    RedactJsonNode(item);
                }
            }
        }
    }
}
