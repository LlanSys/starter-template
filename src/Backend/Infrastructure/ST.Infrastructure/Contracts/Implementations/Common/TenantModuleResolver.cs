using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class TenantModuleResolver : ITenantModuleResolver
{
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly IMemoryCache _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ShowcaseSettings _showcaseSettings;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public TenantModuleResolver(
        ICurrentTenantProvider tenantProvider,
        IMemoryCache cache,
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<ST.Infrastructure.Configuration.ShowcaseSettings> showcaseSettings)
    {
        _tenantProvider = tenantProvider;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _showcaseSettings = showcaseSettings.Value;
    }

    public async Task<IReadOnlyList<string>> GetEnabledModulesAsync(CancellationToken cancellationToken = default)
    {
        var globalModules = new List<string>();
        if (_showcaseSettings.EnableGlobalShowcase)
        {
            globalModules.Add("Showcase");
        }

        Guid tenantId;
        try
        {
            tenantId = _tenantProvider.TenantId;
        }
        catch (InvalidOperationException)
        {
            return globalModules;
        }

        if (tenantId == Guid.Empty)
        {
            return globalModules;
        }

        var cacheKey = $"tenant_modules_{tenantId}";
        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModules) && cachedModules != null)
        {
            return cachedModules.Concat(globalModules).Distinct().ToList();
        }

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();

        var modules = await unitOfWork.Tenants.FindAll()
            .Where(t => t.Id == tenantId)
            .SelectMany(t => t.Modules)
            .Where(m => m.IsActive)
            .Select(m => m.ModuleKey)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _cache.Set(cacheKey, modules, CacheDuration);
        return modules.Concat(globalModules).Distinct().ToList();
    }
}
