using ST.SharedKernel.Features.ControlPlane.Tenants;
namespace ST.Tests.Unit;

public sealed class FrontendOriginTests
{
    [Theory]
    [InlineData("https://APP.Example.test:443/", "https://app.example.test")]
    [InlineData("http://alder.localhost:8181", "http://alder.localhost:8181")]
    [InlineData("http://127.0.0.1:8181", "http://127.0.0.1:8181")]
    [InlineData("https://app.example.test.:8443", "https://app.example.test:8443")]
    public void CanonicalizesSupportedOrigins(string value, string expected)
    {
        Assert.True(FrontendOrigin.TryNormalize(value, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://app.example.test")]
    [InlineData("https://user:password@app.example.test")]
    [InlineData("https://app.example.test/path")]
    [InlineData("https://app.example.test?workspace=other")]
    [InlineData("https://app.example.test#fragment")]
    [InlineData("ftp://localhost")]
    [InlineData("https://app.example.test\n")]
    [InlineData("https://app.example.test\\another")]
    public void RejectsNonOriginsAndUnsafeAddresses(string? value)
    {
        Assert.False(FrontendOrigin.TryNormalize(value, out var actual));
        Assert.Null(actual);
    }
}
