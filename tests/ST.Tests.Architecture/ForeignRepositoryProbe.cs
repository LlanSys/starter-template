using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;

namespace ST.Tests.Architecture.Features.BoundaryProbe;

internal static class ForeignRepositoryProbe
{
    internal static object? Resolve(IServiceProvider services)
    {
        return services.GetService(typeof(ITenantRepository));
    }
}
