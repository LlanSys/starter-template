using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ST.Api.Common.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        if (!policyName.StartsWith(PermissionAuthorizationConstants.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return await base.GetPolicyAsync(policyName).ConfigureAwait(false);
        }

        var permission = policyName[PermissionAuthorizationConstants.PolicyPrefix.Length..];
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }
}
