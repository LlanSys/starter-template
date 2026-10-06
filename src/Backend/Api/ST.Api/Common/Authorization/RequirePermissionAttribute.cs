using Microsoft.AspNetCore.Authorization;

namespace ST.Api.Common.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionAuthorizationConstants.BuildPolicyName(permission);
    }
}
