using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ST.Api.Configuration;

namespace ST.Api.Common.Authorization;

internal static class StampProvisioningAuthentication
{
    public const string Scheme = "GitHubStampProvisioning";
    public const string Policy = "StampProvisioning.Complete";

    public static void AddStampProvisioningAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(StampProvisioningAuthenticationSettings.SectionName)
            .Get<StampProvisioningAuthenticationSettings>() ?? new();
        if (settings.Enabled && (string.IsNullOrWhiteSpace(settings.Audience)
            || string.IsNullOrWhiteSpace(settings.Repository) || string.IsNullOrWhiteSpace(settings.RepositoryId)
            || string.IsNullOrWhiteSpace(settings.Subject) || string.IsNullOrWhiteSpace(settings.Ref)))
        {
            throw new InvalidOperationException("Configure the exact repository, immutable repository ID, subject, ref and audience for provisioning authentication.");
        }

        services.AddAuthentication().AddJwtBearer(Scheme, options =>
        {
            options.Authority = "https://token.actions.githubusercontent.com";
            options.Audience = settings.Audience;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://token.actions.githubusercontent.com",
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
        services.AddAuthorization(options => options.AddPolicy(Policy, policy =>
        {
            policy.AddAuthenticationSchemes(Scheme);
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => settings.Enabled
                && context.User.HasClaim("repository", settings.Repository)
                && context.User.HasClaim("repository_id", settings.RepositoryId)
                && context.User.HasClaim("sub", settings.Subject)
                && context.User.HasClaim("ref", settings.Ref)
                && context.User.HasClaim("workflow_ref",
                    settings.Repository + "/.github/workflows/provision-isolated-stamp.yml@" + settings.Ref)
                && context.User.HasClaim(claim => claim.Type == "run_id"
                    && long.TryParse(claim.Value, out var runId) && runId > 0));
        }));
    }
}
