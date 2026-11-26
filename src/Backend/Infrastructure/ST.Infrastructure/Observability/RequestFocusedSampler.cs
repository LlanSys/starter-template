using System.Diagnostics;
using OpenTelemetry.Trace;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Observability;

/// <summary>
/// Keeps every incoming request with all the spans it causes, and samples work that does not belong
/// to a request. Spans with a parent follow the parent's decision (see <see cref="Create"/>), so a
/// request trace is always complete. Only root spans reach this sampler:
/// <list type="bullet">
/// <item>Server (incoming request): always recorded.</item>
/// <item>Client (database, storage or HTTP call outside a request, e.g. background polling):
/// sampled at <see cref="TraceSamplingSettings.OutboundWithoutRequestRatio"/>.</item>
/// <item>Anything else (jobs, publishing, internal activities): sampled at
/// <see cref="TraceSamplingSettings.BackgroundRatio"/>.</item>
/// </list>
/// </summary>
public sealed class RequestFocusedSampler : Sampler
{
    private static readonly SamplingResult Keep = new(SamplingDecision.RecordAndSample);

    private readonly Sampler _outboundWithoutRequest;
    private readonly Sampler _background;

    public RequestFocusedSampler(TraceSamplingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _outboundWithoutRequest = new TraceIdRatioBasedSampler(ValidRatio(settings.OutboundWithoutRequestRatio, nameof(settings.OutboundWithoutRequestRatio)));
        _background = new TraceIdRatioBasedSampler(ValidRatio(settings.BackgroundRatio, nameof(settings.BackgroundRatio)));
        Description = $"RequestFocusedSampler{{outbound={settings.OutboundWithoutRequestRatio},background={settings.BackgroundRatio}}}";
    }

    /// <summary>Creates the sampler wrapped so that child spans follow their parent's decision.</summary>
    public static Sampler Create(TraceSamplingSettings settings) => new ParentBasedSampler(new RequestFocusedSampler(settings));

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) => samplingParameters.Kind switch
    {
        ActivityKind.Server => Keep,
        ActivityKind.Client => _outboundWithoutRequest.ShouldSample(samplingParameters),
        _ => _background.ShouldSample(samplingParameters)
    };

    private static double ValidRatio(double ratio, string name) =>
        ratio is >= 0 and <= 1
            ? ratio
            : throw new InvalidOperationException($"Observability:Sampling:{name} must be between 0 and 1.");
}
