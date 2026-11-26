namespace ST.Infrastructure.Configuration;

/// <summary>
/// Trace sampling for OTLP export. Incoming requests are always traced, together with every span
/// they cause (database, storage and HTTP calls). Work that does not belong to a request, such as
/// background polling, is sampled with the ratios below. Applies when OTLP is used without Azure
/// Monitor; Azure Monitor keeps its own sampler.
/// </summary>
public sealed class TraceSamplingSettings
{
    /// <summary>
    /// Share (0 to 1) of outbound calls (database, storage, HTTP) made outside any request to keep.
    /// Default 0: background polling queries are not traced.
    /// </summary>
    public double OutboundWithoutRequestRatio { get; set; }

    /// <summary>
    /// Share (0 to 1) of other background work outside any request (jobs, publishing, internal
    /// activities) to keep. Default 0.1.
    /// </summary>
    public double BackgroundRatio { get; set; } = 0.1;
}
