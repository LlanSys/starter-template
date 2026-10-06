using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Queries;



internal sealed class GetNavigationMenusQueryHandler(
    IIamUnitOfWork unitOfWork,
    ITenantModuleResolver moduleResolver,
    ILogger<GetNavigationMenusQueryHandler> logger)
    : IRequestHandler<GetNavigationMenusQuery, AppResponse<IReadOnlyList<MenuResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<MenuResponse>>> Handle(GetNavigationMenusQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var permissionSet = query.PermissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var enabledModules = await moduleResolver.GetEnabledModulesAsync(cancellationToken).ConfigureAwait(false);
            var moduleSet = enabledModules.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var menus = await unitOfWork.MenuRepository
                .ListAsync(
                    menus => menus
                        .Where(menu => menu.IsActive && menu.Placement == query.Placement)
                        .Where(menu =>
                            menu.RequiredPermissionKey == "control_plane.manage"
                                ? permissionSet.Contains(menu.RequiredPermissionKey)
                                : (query.HasFullAccess || menu.RequiredPermissionKey == null || permissionSet.Contains(menu.RequiredPermissionKey)))
                        .Where(menu => menu.RequiredModule == null || moduleSet.Contains(menu.RequiredModule))
                        .OrderBy(static menu => menu.DisplayOrder).ThenBy(static menu => menu.Title),
                    cancellationToken)
                .ConfigureAwait(false);

            return AppResponses.Success<IReadOnlyList<MenuResponse>>(menus.ToTree());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(GetNavigationMenusQueryHandler), ex);
            throw;
        }
    }
}
