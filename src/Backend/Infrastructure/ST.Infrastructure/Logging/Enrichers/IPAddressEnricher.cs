using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging.Enrichers;

/// <summary>
/// Adds <c>IPAddress</c> and <c>X-Forwarded-For</c> properties to every log event.
/// </summary>
internal sealed class IPAddressEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IPAddressEnricher() : this(new HttpContextAccessor()) { }

    public IPAddressEnricher(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;
        var remote = context?.Connection.RemoteIpAddress;

        if (remote is null) return;

        var ip = remote.IsIPv4MappedToIPv6
            ? remote.MapToIPv4().ToString()
            : remote.ToString();

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IPAddress", ip));

        var forwarded = context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("X-Forwarded-For", forwarded));
      }
}
