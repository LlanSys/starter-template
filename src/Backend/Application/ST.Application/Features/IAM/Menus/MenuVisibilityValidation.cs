using ST.Domain.Features.IAM.Contracts;
namespace ST.Application.Features.IAM.Menus;

internal static class MenuVisibilityValidation
{
    public static async Task<string?> ValidateAsync(IIamUnitOfWork unitOfWork, IReadOnlyList<string>? roles,
        IReadOnlyList<string>? permissions, CancellationToken cancellationToken, ST.Application.Features.ControlPlane.Modules.EffectiveModuleAccess? access = null)
    {
        var roleNames = roles?.Select(value => value.Trim().ToUpperInvariant()).Distinct().ToArray() ?? [];
        if (roleNames.Length > 0)
        {
            var found = await unitOfWork.RoleRepository.ListAsync(query => query.Where(role => roleNames.Contains(role.NormalizedName!)), cancellationToken).ConfigureAwait(false);
            if (found.Count != roleNames.Length) return "One or more selected roles are no longer available.";
        }
        var keys = permissions?.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
        if (keys.Length > 0)
        {
            var found = await unitOfWork.PermissionRepository.ListAsync(query => query.Where(permission => permission.IsActive && keys.Contains(permission.Key)), cancellationToken).ConfigureAwait(false);
            var available = found.Select(x => x.Key).Concat(access?.Catalogue.Where(x => x.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Permission && access.EntryAvailable(x)).Select(x => x.Key) ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (keys.Any(key => !available.Contains(key))) return "One or more selected permissions are no longer available.";
        }
        return null;
    }
}
