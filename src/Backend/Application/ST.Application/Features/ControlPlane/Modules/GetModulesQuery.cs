using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
using ST.Application.Contracts.Interfaces.Common;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed record GetModulesQuery : IRequest<AppResponse<IReadOnlyList<ModuleResponse>>>, ICachableRequest
{
    public string CacheGroup => "modules";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
