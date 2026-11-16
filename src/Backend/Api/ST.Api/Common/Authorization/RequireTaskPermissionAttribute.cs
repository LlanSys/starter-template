using Microsoft.AspNetCore.Authorization;

namespace ST.Api.Common.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireTaskPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyName = "Dashboard.TaskAction";

    public RequireTaskPermissionAttribute()
    {
        Policy = PolicyName;
    }
}
