using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class GetModuleCatalogueHandler(IControlPlaneUnitOfWork unit) : IRequestHandler<GetModuleCatalogueQuery, AppResponse<IReadOnlyList<CatalogueEntryResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<CatalogueEntryResponse>>> Handle(GetModuleCatalogueQuery request, CancellationToken cancellationToken)
    {
        var key = request.ModuleKey.Trim().ToLowerInvariant();
        var rows = await unit.Catalogue.ListAsync(q => q.Where(x => x.ModuleKey == key).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Title), cancellationToken).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<CatalogueEntryResponse>>(rows.Select(x => x.ToResponse()).ToArray());
    }
}
