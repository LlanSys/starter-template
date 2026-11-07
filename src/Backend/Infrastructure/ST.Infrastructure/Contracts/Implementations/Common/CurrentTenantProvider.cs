using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Exceptions;
using ST.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class CurrentTenantProvider(
    IHttpContextAccessor httpContextAccessor,
    IOptions<OrgSettings> options, WorkspaceTenantOverride? workspace = null) : ICurrentTenantProvider
{
    public Guid TenantId
    {
        get
        {
            if (workspace?.TenantId is Guid scopedTenant) return scopedTenant;
            var settings = options.Value;
            var user = httpContextAccessor.HttpContext?.User;

            if (TryGetTenantId(user?.FindFirstValue("tenant_id"), out var claimTenantId))
            {
                return claimTenantId;
            }

            var headerValue = httpContextAccessor.HttpContext?.Request.Headers[settings.HeaderName].FirstOrDefault();
            if (TryGetTenantId(headerValue, out var headerTenantId))
            {
                return headerTenantId;
            }

            if (settings.DefaultTenantId != Guid.Empty)
            {
                return settings.DefaultTenantId;
            }

            throw new TenantNotResolvedException();
        }
    }

    private static bool TryGetTenantId(string? value, out Guid tenantId)
    {
        return Guid.TryParse(value, out tenantId) && tenantId != Guid.Empty;
    }
}
