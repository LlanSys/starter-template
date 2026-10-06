using System.Diagnostics.Metrics;

namespace ST.Api.Logging;

public static class TenantMetrics
{
    public const string MeterName = "ST.TenantMetrics";

    public static readonly Meter Meter = new(MeterName, "1.0.0");

    // Tracks the number of API requests processed per tenant
    public static readonly Counter<long> ApiRequestCounter = Meter.CreateCounter<long>(
        "tenant_api_requests_total",
        description: "The total number of API requests handled, tagged by tenant_id");

    // Tracks the number of bytes uploaded (example resource usage metric)
    public static readonly Counter<long> FileUploadBytesCounter = Meter.CreateCounter<long>(
        "tenant_upload_bytes_total",
        unit: "By",
        description: "The total bytes uploaded by the tenant");
}
