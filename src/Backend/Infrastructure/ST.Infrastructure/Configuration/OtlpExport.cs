using OpenTelemetry.Exporter;

namespace ST.Infrastructure.Configuration;

/// <summary>
/// Shared OTLP/HTTP export settings for traces, metrics and logs.
/// Hosted OTLP gateways such as Grafana Cloud accept HTTP/protobuf at a base URL
/// (for example <c>https://otlp-gateway-prod-eu-west-2.grafana.net/otlp</c>) and expect
/// each signal under <c>/v1/traces</c>, <c>/v1/metrics</c> or <c>/v1/logs</c>.
/// Exporters configured in code do not append those paths, so they are built here.
/// </summary>
public static class OtlpExport
{
    public const string Traces = "traces";
    public const string Metrics = "metrics";
    public const string Logs = "logs";

    private static readonly string[] SignalSuffixes = ["/v1/traces", "/v1/metrics", "/v1/logs"];

    /// <summary>
    /// Returns the full HTTP endpoint for one signal. Accepts the base URL, or a URL that
    /// already ends with any signal path (which is replaced).
    /// </summary>
    public static Uri SignalEndpoint(string baseEndpoint, string signal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(signal);

        var trimmed = baseEndpoint.Trim().TrimEnd('/');

        foreach (var suffix in SignalSuffixes)
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^suffix.Length];
                break;
            }
        }

        var endpoint = new Uri(trimmed + "/v1/" + signal, UriKind.Absolute);

        if (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp)
        {
            throw new InvalidOperationException("Observability:Otlp:Endpoint must be an http or https URL.");
        }

        return endpoint;
    }

    /// <summary>
    /// Parses <c>key=value</c> pairs separated by commas. Values are percent-decoded, so the
    /// OpenTelemetry environment-variable form (<c>Authorization=Basic%20abc</c>) and the plain
    /// form (<c>Authorization=Basic abc</c>) are equivalent. Only the first '=' separates the key,
    /// so base64 padding in values is preserved.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ParseHeaders(string? headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(headers))
        {
            return result;
        }

        foreach (var pair in headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);

            if (separator <= 0 || separator == pair.Length - 1)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separator].Trim());
            var value = Uri.UnescapeDataString(pair[(separator + 1)..].Trim());

            if (key.Length > 0 && value.Length > 0)
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>Formats parsed headers for <c>OtlpExporterOptions.Headers</c>.</summary>
    public static string FormatHeaders(IReadOnlyDictionary<string, string> headers) =>
        string.Join(',', headers.Select(header => header.Key + "=" + header.Value));

    /// <summary>
    /// Configures an OpenTelemetry SDK exporter for one signal: HTTP/protobuf (the exporter's default is
    /// gRPC), the full signal endpoint, and decoded headers.
    /// </summary>
    public static void ConfigureExporter(OtlpExporterOptions options, OtlpSettings settings, string signal)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Endpoint = SignalEndpoint(settings.Endpoint, signal);

        var headers = ParseHeaders(settings.Headers);

        if (headers.Count > 0)
        {
            options.Headers = FormatHeaders(headers);
        }
    }
}
