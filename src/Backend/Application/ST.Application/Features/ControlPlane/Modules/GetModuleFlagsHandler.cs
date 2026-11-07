using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class GetModuleFlagsHandler(IControlPlaneUnitOfWork unit) : IRequestHandler<GetModuleFlagsQuery, AppResponse<IReadOnlyList<ModuleFlagResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<ModuleFlagResponse>>> Handle(GetModuleFlagsQuery request, CancellationToken cancellationToken)
    {
        var key = request.ModuleKey.Trim().ToLowerInvariant();
        var rows = await unit.FeatureFlags.ListAsync(q => q.Where(x => x.ModuleKey == key), cancellationToken).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<ModuleFlagResponse>>(rows.Select(x => new ModuleFlagResponse(x.ScopeTenantId, x.ModuleKey, x.Key, x.Name, x.IsEnabled)).ToArray());
    }
}
