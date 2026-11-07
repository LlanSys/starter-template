using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace ST.Api.Common.Authorization;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RequireGraphQLPermissionAttribute(string permission) : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context, IObjectFieldDescriptor descriptor, MemberInfo? member)
    {
        descriptor.Authorize(PermissionAuthorizationConstants.BuildPolicyName(permission));
    }
}
