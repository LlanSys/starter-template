using System.Net;
using System.Net.Http.Headers;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class TenantIngressBoundaryTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task KnownHostAndExplicitSelectorMustAgreeWithSignedWorkspace()
    {
        var tenant = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        await factory.RegisterTenantAsync(other);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = $"{tenant:N}.invalid";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "users.view");
        client.DefaultRequestHeaders.Add("X-Test-TenantId", other.ToString());

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/iam/admin/users")).StatusCode);

        client.DefaultRequestHeaders.Add("X-Tenant-Id", other.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/iam/admin/users")).StatusCode);

        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Remove("X-Test-TenantId");
        client.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/iam/admin/users")).StatusCode);

        client.DefaultRequestHeaders.Host = "unknown-workspace.invalid";
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/iam/admin/users")).StatusCode);
    }

    [Fact]
    public async Task ForwardedHostFromUntrustedPeerCannotSelectAWorkspace()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        var response = await factory.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");
            context.Request.Method = "GET";
            context.Request.Scheme = "https";
            context.Request.Host = new("unknown-workspace.invalid");
            context.Request.Path = "/api/v1/iam/admin/users";
            context.Request.Headers["X-Forwarded-Host"] = $"{tenant:N}.invalid";
            context.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            context.Request.Headers["X-Forwarded-Proto"] = "https";
        });

        Assert.Equal((int)HttpStatusCode.NotFound, response.Response.StatusCode);
    }
}
