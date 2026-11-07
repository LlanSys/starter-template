using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
using ST.Application.Contracts.Interfaces.Common;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed record GetAvailableCatalogueQuery() : IRequest<AppResponse<IReadOnlyList<CatalogueEntryResponse>>>, ICachableRequest
{
    public string CacheGroup => "module-catalogue";
    public string Discriminator => "GetAvailableCatalogueQuery";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
