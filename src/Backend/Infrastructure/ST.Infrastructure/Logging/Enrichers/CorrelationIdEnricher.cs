using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging.Enrichers;

// ─────────────────────────────────────────────────────────────────────────────
// Enrichers — internal: these are infrastructure plumbing, not public API (CA1515)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Adds a <c>CorrelationId</c> property to every log event.
/// </summary>
/// <remarks>
/// Priority order: X-Correlation-ID request header → ASP.NET TraceIdentifier → new GUID.
/// NOTE: <c>Serilog.Enrichers.CorrelationId</c> NuGet package provides similar functionality.
/// This custom enricher is kept because it also sets TraceIdentifier as a fallback,
/// ensuring the same ID appears in both Serilog output and ASP.NET diagnostic logs.
/// </remarks>
internal sealed class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string CorrelationIdProperty = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    // Parameterless ctor for cases where DI hasn't run yet (bootstrap logger)
    public CorrelationIdEnricher() : this(new HttpContextAccessor()) { }

    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;

        var correlationId =
            context?.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? context?.TraceIdentifier
            ?? Guid.CreateVersion7().ToString();

        // Write back so TraceIdentifier and the header stay in sync
        if (context is not null)
        {
            context.TraceIdentifier = correlationId;
            if (!context.Response.HasStarted)
            {
                context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
            }
        }

        logEvent.AddOrUpdateProperty(
            propertyFactory.CreateProperty(CorrelationIdProperty, correlationId));
    }
}
