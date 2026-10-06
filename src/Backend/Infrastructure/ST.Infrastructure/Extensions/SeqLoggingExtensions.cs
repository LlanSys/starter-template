using System.Globalization;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace ST.Infrastructure.Extensions;

public static class SeqLoggingExtensions
{
    public static IHostBuilder UseSeqLogging(this IHostBuilder host)
    {
        return host.UseSerilog((context, services, configuration) =>
        {
            _ = configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "ST.Application")
                .Enrich.WithProperty("Service", "ClientService")
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.Seq(
                    context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
                    apiKey: context.Configuration["Seq:ApiKey"],
                    batchPostingLimit: 50,
                    period: TimeSpan.FromSeconds(5),
                    formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .Filter.ByExcluding(logEvent =>
                    logEvent.Level < LogEventLevel.Information &&
                    logEvent.Properties.TryGetValue("MessageType", out var messageTypeValue) &&
                    messageTypeValue is ScalarValue { Value: string messageType } &&
                    messageType.Contains("HealthCheck", StringComparison.OrdinalIgnoreCase));
        });
    }
}
