namespace ST.Domain.Shared.Exceptions;

/// <summary>
/// Thrown by <see cref="ST.Domain.Shared.Contracts.Common.ICurrentTenantProvider"/> when
/// no tenant can be resolved from the current request context (no claim, no header, no default).
/// Catching this specific type allows callers to distinguish an expected "no-tenant" scenario
/// from unexpected <see cref="InvalidOperationException"/> failures elsewhere in the call chain.
/// </summary>
public sealed class TenantNotResolvedException : InvalidOperationException
{
    public TenantNotResolvedException()
        : base("No tenant could be resolved from claims, request headers, or the configured default tenant.")
    {
    }

    public TenantNotResolvedException(string message)
        : base(message)
    {
    }
}
