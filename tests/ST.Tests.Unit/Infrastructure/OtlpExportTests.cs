using OpenTelemetry.Exporter;
using ST.Infrastructure.Configuration;

namespace ST.Tests.Unit.Infrastructure;

public sealed class OtlpExportTests
{
    private const string GrafanaBase = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp";

    [Theory]
    [InlineData(GrafanaBase, OtlpExport.Traces, GrafanaBase + "/v1/traces")]
    [InlineData(GrafanaBase + "/", OtlpExport.Metrics, GrafanaBase + "/v1/metrics")]
    [InlineData("  " + GrafanaBase + "  ", OtlpExport.Logs, GrafanaBase + "/v1/logs")]
    [InlineData(GrafanaBase + "/v1/logs", OtlpExport.Traces, GrafanaBase + "/v1/traces")]
    [InlineData("http://localhost:4318", OtlpExport.Metrics, "http://localhost:4318/v1/metrics")]
    public void SignalEndpointAppendsOrReplacesTheSignalPath(string configured, string signal, string expected) =>
        Assert.Equal(new Uri(expected), OtlpExport.SignalEndpoint(configured, signal));

    [Fact]
    public void SignalEndpointRejectsNonHttpSchemes() =>
        Assert.Throws<InvalidOperationException>(() => OtlpExport.SignalEndpoint("ftp://collector/otlp", OtlpExport.Traces));

    [Theory]
    [InlineData("Authorization=Basic%20MTIzNDU2OmdsY190b2tlbg==")]
    [InlineData("Authorization=Basic MTIzNDU2OmdsY190b2tlbg==")]
    [InlineData(" Authorization = Basic MTIzNDU2OmdsY190b2tlbg== ")]
    public void ParseHeadersAcceptsEncodedAndPlainFormsAndKeepsBase64Padding(string configured)
    {
        var headers = OtlpExport.ParseHeaders(configured);

        Assert.Equal("Basic MTIzNDU2OmdsY190b2tlbg==", Assert.Single(headers, header => header.Key == "Authorization").Value);
    }

    [Fact]
    public void ParseHeadersSupportsSeveralPairsAndSkipsMalformedOnes()
    {
        var headers = OtlpExport.ParseHeaders("Authorization=Basic abc,X-Scope-OrgID=42,broken,=novalue,empty=");

        Assert.Equal(2, headers.Count);
        Assert.Equal("Basic abc", headers["Authorization"]);
        Assert.Equal("42", headers["X-Scope-OrgID"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseHeadersReturnsEmptyForMissingConfiguration(string? configured) =>
        Assert.Empty(OtlpExport.ParseHeaders(configured));

    [Fact]
    public void ConfigureExporterUsesHttpProtobufTheSignalEndpointAndDecodedHeaders()
    {
        var options = new OtlpExporterOptions();

        OtlpExport.ConfigureExporter(
            options,
            new OtlpSettings { Endpoint = GrafanaBase, Headers = "Authorization=Basic%20abc==" },
            OtlpExport.Traces);

        Assert.Equal(OtlpExportProtocol.HttpProtobuf, options.Protocol);
        Assert.Equal(new Uri(GrafanaBase + "/v1/traces"), options.Endpoint);
        Assert.Equal("Authorization=Basic abc==", options.Headers);
    }
}
