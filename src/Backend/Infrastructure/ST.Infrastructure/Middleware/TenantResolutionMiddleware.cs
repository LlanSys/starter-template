using ST.SharedKernel.Features.ControlPlane.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next, IOptions<OrgSettings> options)
{
    public const string ResolvedTenantItem = "ResolvedRequestTenant";
    public const string FrontendTenantItem = "ResolvedFrontendTenant";

    public async Task InvokeAsync(HttpContext context, IControlPlaneUnitOfWork controlPlane)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/ready", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var settings = options.Value;
        var headers = context.Request.Headers;
        var host = context.Request.Host.Host.TrimEnd('.').ToLowerInvariant();
        Guid? selectedTenant = null;

        if (headers.TryGetValue(settings.HeaderName, out var values))
        {
            if (values.Count != 1 || !Guid.TryParse(values[0], out var selector) || selector == Guid.Empty)
            {
                await Reject(context, StatusCodes.Status400BadRequest, "The workspace selector is invalid.").ConfigureAwait(false);
                return;
            }

            selectedTenant = selector;
        }

        if (headers.TryGetValue(FrontendOrigin.HeaderName, out var frontendValues))
        {
            if (frontendValues.Count != 1 || !FrontendOrigin.TryNormalize(frontendValues[0], out var origin))
            {
                await Reject(context, StatusCodes.Status400BadRequest, "The organization address is invalid.").ConfigureAwait(false);
                return;
            }
            var matches = await controlPlane.Tenants.ListAsync(query => query
                .Where(tenant => tenant.DedicatedFrontendOrigin == origin && !tenant.IsDemo)
                .Select(tenant => new { tenant.Id, tenant.Status, tenant.DeploymentStampId }).Take(2), context.RequestAborted).ConfigureAwait(false);

            if (matches.Count > 0)
            {
                if (matches.Count != 1 || matches[0].Status != TenantStatus.Active
                    || (settings.DeploymentStampId.HasValue && matches[0].DeploymentStampId != settings.DeploymentStampId))
                {
                    await Reject(context, StatusCodes.Status404NotFound, "This organization address is unavailable.").ConfigureAwait(false);
                    return;
                }

                if (selectedTenant.HasValue && selectedTenant != matches[0].Id)
                {
                    await Reject(context, StatusCodes.Status400BadRequest, "The organization selection conflicts with this address.").ConfigureAwait(false);
                    return;
                }
                selectedTenant = matches[0].Id;
                context.Items[FrontendTenantItem] = selectedTenant.Value;
            }
            else if (!settings.CentralFrontendOrigins.Any(shared => FrontendOrigin.TryNormalize(shared, out var canonical)
                && string.Equals(canonical, origin, StringComparison.Ordinal)))
            {
                await Reject(context, StatusCodes.Status404NotFound, "This organization address is unavailable.").ConfigureAwait(false);
                return;
            }
        }

        if (!settings.CentralApiHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            var tenants = await controlPlane.Tenants.ListAsync(query => query
                .Where(item => item.HostName.ToLower() == host
                    && (!settings.DeploymentStampId.HasValue || item.DeploymentStampId == settings.DeploymentStampId))
                .Select(item => new { item.Id, item.Status }).Take(2), context.RequestAborted).ConfigureAwait(false);

            if (tenants.Count != 1 || tenants[0].Status != TenantStatus.Active)
            {
                await Reject(context, StatusCodes.Status404NotFound, "This workspace host is unavailable.").ConfigureAwait(false);
                return;
            }

            var tenant = tenants[0];

            if (selectedTenant is Guid supplied && supplied != tenant.Id)
            {
                await Reject(context, StatusCodes.Status400BadRequest, "The workspace selector conflicts with the request host.").ConfigureAwait(false);
                return;
            }

            selectedTenant = tenant.Id;
        }
        else if (selectedTenant is Guid supplied && !await controlPlane.Tenants
            .AnyAsync(item => item.Id == supplied && item.Status == TenantStatus.Active
                && (!settings.DeploymentStampId.HasValue || item.DeploymentStampId == settings.DeploymentStampId), context.RequestAborted).ConfigureAwait(false))
        {
            await Reject(context, StatusCodes.Status404NotFound, "This workspace is unavailable.").ConfigureAwait(false);
            return;
        }

        if (selectedTenant is Guid tenantId)
        {
            context.Items[ResolvedTenantItem] = tenantId;
            headers[settings.HeaderName] = tenantId.ToString();
        }

        await next(context).ConfigureAwait(false);
    }

    private static Task Reject(HttpContext context, int status, string title)
    {
        return Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
    }
}
