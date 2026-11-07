using ST.Api.Common.Authorization;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;
namespace ST.Api.Middleware;
internal sealed class ModuleAvailabilityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IEffectiveModulePolicy policy)
    {
        var binding = context.GetEndpoint()?.Metadata.GetMetadata<RequireModuleAttribute>();
        if (binding is not null && context.User.Identity?.IsAuthenticated == true)
        {
            var access = await policy.GetAsync(context.RequestAborted).ConfigureAwait(false);
            var pages = access.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Page && binding.PagePath is not null && x.Path == binding.PagePath).ToArray();
            var full = context.User.IsInRole("System Administrator");
            var permissions = context.User.FindAll("permission").Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!access.Modules.Contains(binding.ModuleKey) || pages.Any(page => !access.EntryAvailable(page) || page.PermissionKeys.Any(key => !access.PermissionAvailable(key) || (!full && !permissions.Contains(key)))))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "This module or page is not enabled for your workspace." }, context.RequestAborted).ConfigureAwait(false);
                return;
            }
        }
        await next(context).ConfigureAwait(false);
    }
}
