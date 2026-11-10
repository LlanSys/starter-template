using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class GetAvailableCatalogueHandler(IEffectiveModulePolicy policy) : IRequestHandler<GetAvailableCatalogueQuery, AppResponse<IReadOnlyList<CatalogueEntryResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<CatalogueEntryResponse>>> Handle(GetAvailableCatalogueQuery request, CancellationToken cancellationToken)
    {
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<CatalogueEntryResponse>>(access.Catalogue.Where(access.EntryAvailable).Select(x => x.ToResponse()).ToArray());
    }
}
