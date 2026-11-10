using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
namespace ST.Application.Features.IAM.Menus;
public interface IEffectiveNavigation
{
    Task<IReadOnlyList<ResolvedNavigationItem>> GetAsync(CancellationToken cancellationToken = default);
    Task<AppResponse<MenuResponse>> SaveOverrideAsync(string pageKey, UpdateMenuRequest request, string actor, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> ResetOverrideAsync(string pageKey, CancellationToken cancellationToken = default);
}
