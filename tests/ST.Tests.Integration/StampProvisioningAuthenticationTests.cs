using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ST.Api.Common.Authorization;

namespace ST.Tests.Integration;

public sealed class StampProvisioningAuthenticationTests
{
    [Fact]
    public async Task CallbackRequiresTheSignedAudienceRepositoryEnvironmentRefAndWorkflow()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "fixture-key" };
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ControlPlane:Provisioning:Authentication:Enabled"] = "true",
            ["ControlPlane:Provisioning:Authentication:Repository"] = "fixture/starter",
            ["ControlPlane:Provisioning:Authentication:RepositoryId"] = "12345",
            ["ControlPlane:Provisioning:Authentication:Subject"] = "repo:fixture/starter:environment:staging"
        });
        builder.Services.AddStampProvisioningAuthentication(builder.Configuration);
        builder.Services.PostConfigure<JwtBearerOptions>(StampProvisioningAuthentication.Scheme, options =>
        {
            var metadata = new OpenIdConnectConfiguration { Issuer = "https://token.actions.githubusercontent.com" };
            metadata.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        });
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPost("/complete", () => Results.Ok()).RequireAuthorization(StampProvisioningAuthentication.Policy);
        await app.StartAsync();
        using var client = app.GetTestClient();

        string Token(string? changedClaim = null, string? value = null, string? issuer = null,
            string? audience = null, bool expired = false)
        {
            var claims = new Dictionary<string, string>
            {
                ["repository"] = "fixture/starter",
                ["repository_id"] = "12345",
                ["sub"] = "repo:fixture/starter:environment:staging",
                ["ref"] = "refs/heads/main",
                ["workflow_ref"] = "fixture/starter/.github/workflows/provision-isolated-stamp.yml@refs/heads/main",
                ["run_id"] = "123"
            };
            if (changedClaim is not null) { claims[changedClaim] = value!; }
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer ?? "https://token.actions.githubusercontent.com",
                audience ?? "urn:startertemplate:provisioning",
                claims.Select(pair => new Claim(pair.Key, pair.Value)),
                now.AddMinutes(-10), expired ? now.AddMinutes(-5) : now.AddMinutes(5),
                new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        async Task AssertStatus(string? token, HttpStatusCode expected)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/complete");
            if (token is not null) { request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); }
            using var response = await client.SendAsync(request);
            Assert.Equal(expected, response.StatusCode);
        }

        await AssertStatus(null, HttpStatusCode.Unauthorized);
        await AssertStatus(Token(), HttpStatusCode.OK);
        await AssertStatus(Token("repository", "other/starter"), HttpStatusCode.Forbidden);
        await AssertStatus(Token("repository_id", "99999"), HttpStatusCode.Forbidden);
        await AssertStatus(Token("sub", "repo:fixture/starter:environment:other"), HttpStatusCode.Forbidden);
        await AssertStatus(Token("ref", "refs/heads/unreviewed"), HttpStatusCode.Forbidden);
        await AssertStatus(Token("workflow_ref", "fixture/starter/.github/workflows/other.yml@refs/heads/main"), HttpStatusCode.Forbidden);
        await AssertStatus(Token("run_id", "0"), HttpStatusCode.Forbidden);
        await AssertStatus(Token(audience: "different-audience"), HttpStatusCode.Unauthorized);
        await AssertStatus(Token(issuer: "https://example.invalid"), HttpStatusCode.Unauthorized);
        await AssertStatus(Token(expired: true), HttpStatusCode.Unauthorized);
        await AssertStatus(Token()[..^8] + "tampered", HttpStatusCode.Unauthorized);
    }
}
