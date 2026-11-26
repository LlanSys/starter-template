using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Observability;

namespace ST.Tests.Unit.Infrastructure;

public sealed class RequestFocusedSamplerTests
{
    private static (TracerProvider Provider, ActivitySource Source) Tracing(double outbound, double background)
    {
        var sourceName = "sampler-test-" + Guid.NewGuid().ToString("N");
        var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .SetSampler(RequestFocusedSampler.Create(new TraceSamplingSettings
            {
                OutboundWithoutRequestRatio = outbound,
                BackgroundRatio = background
            }))
            .Build()!;

        return (provider, new ActivitySource(sourceName));
    }

    private static bool Recorded(Activity? activity) => activity is { Recorded: true };

    [Fact]
    public void IncomingRequestsAndEverythingTheyCauseAreAlwaysRecorded()
    {
        var (provider, source) = Tracing(outbound: 0, background: 0);

        using (provider)
        using (source)
        {
            using var request = source.StartActivity("GET /api/v1/iam/admin/menus", ActivityKind.Server);
            using var query = source.StartActivity("SELECT [MenuItems]", ActivityKind.Client);
            using var step = source.StartActivity("resolve navigation", ActivityKind.Internal);

            Assert.True(Recorded(request));
            Assert.True(Recorded(query));
            Assert.True(Recorded(step));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void OutboundCallsOutsideARequestFollowTheOutboundRatio(double ratio, bool expected)
    {
        var (provider, source) = Tracing(outbound: ratio, background: 1);

        using (provider)
        using (source)
        {
            using var polling = source.StartActivity("SELECT [OutboxMessage]", ActivityKind.Client);

            Assert.Equal(expected, Recorded(polling));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void OtherBackgroundWorkFollowsTheBackgroundRatio(double ratio, bool expected)
    {
        var (provider, source) = Tracing(outbound: 1, background: ratio);

        using (provider)
        using (source)
        {
            using var job = source.StartActivity("provisioning timeout sweep", ActivityKind.Internal);
            using var publish = source.StartActivity("publish outbox", ActivityKind.Producer);

            Assert.Equal(expected, Recorded(job));
        }
    }

    [Fact]
    public void ChildrenOfDroppedBackgroundWorkAreDroppedToo()
    {
        var (provider, source) = Tracing(outbound: 1, background: 0);

        using (provider)
        using (source)
        {
            using var job = source.StartActivity("scheduled job", ActivityKind.Internal);
            using var query = source.StartActivity("SELECT [Triggers]", ActivityKind.Client);

            Assert.False(Recorded(job));
            Assert.False(Recorded(query));
        }
    }

    [Theory]
    [InlineData(-0.1, 0.1)]
    [InlineData(0, 1.5)]
    public void RatiosOutsideZeroToOneAreRejected(double outbound, double background) =>
        Assert.Throws<InvalidOperationException>(() => new RequestFocusedSampler(new TraceSamplingSettings
        {
            OutboundWithoutRequestRatio = outbound,
            BackgroundRatio = background
        }));
}
