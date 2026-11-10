namespace ST.Api.Common.Authorization;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireModuleAttribute(string moduleKey, string? pagePath = null) : Attribute
{
    public string ModuleKey { get; } = moduleKey;
    public string? PagePath { get; } = pagePath;
}
