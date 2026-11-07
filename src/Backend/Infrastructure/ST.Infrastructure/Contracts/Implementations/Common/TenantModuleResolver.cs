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
    private readonly IServiceProvider _serviceProvider;

    public TenantModuleResolver(
        ICurrentTenantProvider tenantProvider,
        IMemoryCache cache,
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<ST.Infrastructure.Configuration.ShowcaseSettings> showcaseSettings)
    {
        _tenantProvider = tenantProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<IReadOnlyList<string>> GetEnabledModulesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        // The current tenant is request scoped; do not resolve the effective policy in the child scope.
        var policy = _serviceProvider.GetRequiredService<ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy>();
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        return access.Modules.ToArray();
    }
}
