using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ST.Infrastructure.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly IMemoryCache _cache;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        var host = context.Request.Host.Host;
        var OrgSettings = serviceProvider.GetRequiredService<IOptions<OrgSettings>>().Value;

        // If the header already exists (e.g. injected by a proxy or explicitly provided), respect it
        if (!context.Request.Headers.ContainsKey(OrgSettings.HeaderName))
        {
            var cacheKey = $"Tenant_Host_{host}";

            if (!_cache.TryGetValue(cacheKey, out Guid tenantId))
            {
                using var scope = serviceProvider.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();

                var tenant = await uow.Tenants
                    .FirstOrDefaultAsync(t => t.HostName == host && t.Status == TenantStatus.Active).ConfigureAwait(false);

                tenantId = tenant?.Id ?? Guid.Empty;
                _cache.Set(cacheKey, tenantId, TimeSpan.FromMinutes(5));
            }

            if (tenantId != Guid.Empty)
            {
                // Inject the tenant ID into the headers so CurrentTenantProvider can read it
                context.Request.Headers[OrgSettings.HeaderName] = tenantId.ToString();
            }
        }

        await _next(context).ConfigureAwait(false);
    }
}
