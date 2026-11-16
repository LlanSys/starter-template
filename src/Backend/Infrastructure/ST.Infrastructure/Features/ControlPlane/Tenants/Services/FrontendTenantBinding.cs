using Microsoft.AspNetCore.Http;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Infrastructure.Middleware;
namespace ST.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed class FrontendTenantBinding(IHttpContextAccessor accessor) : IFrontendTenantBinding
{
    public Guid? TenantId => accessor.HttpContext?.Items[TenantResolutionMiddleware.FrontendTenantItem] as Guid?;
}
