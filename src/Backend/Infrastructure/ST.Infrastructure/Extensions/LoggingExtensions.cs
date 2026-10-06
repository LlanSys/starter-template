using ST.Infrastructure.Logging.Enrichers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using static ST.Infrastructure.Extensions.DependencyInjection;

namespace ST.Infrastructure.Extensions;

/// <summary>
/// Registers logging infrastructure dependencies.
/// </summary>
/// <remarks>
/// This class does NOT configure Serilog. Serilog is configured once, exclusively
/// in <c>Program.cs</c> via <c>builder.Host.UseSerilog()</c>. Configuring it here
/// as well would create a second <c>Log.Logger</c> instance, overwriting the first
/// and losing all enrichers and sinks set in <c>Program.cs</c>.
///
/// This class only registers the DI services that Serilog enrichers depend on.
/// </remarks>
public static class LoggingExtensions
{
    /// <summary>
    /// Registers services required by Serilog enrichers that need DI resolution
    /// (specifically enrichers that depend on <see cref="IHttpContextAccessor"/>).
    /// </summary>
    public static IServiceCollection AddInfrastructureLogging(this IServiceCollection services)
    {
        // IHttpContextAccessor is needed by CorrelationIdEnricher and IPAddressEnricher.
        // Serilog's ReadFrom.Services(services) in UseSerilog() will resolve these enrichers
        // from the DI container, so they must be registered before the host builds.
        services.AddHttpContextAccessor();

        // Register enrichers so Serilog's ReadFrom.Services() can resolve them.
        services.AddSingleton<ILogEventEnricher, CorrelationIdEnricher>();
        services.AddSingleton<ILogEventEnricher, IPAddressEnricher>();

        return services;
    }
}
