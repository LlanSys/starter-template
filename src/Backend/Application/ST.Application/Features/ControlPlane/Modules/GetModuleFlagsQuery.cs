using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed record GetModuleFlagsQuery(string ModuleKey) : IRequest<AppResponse<IReadOnlyList<ModuleFlagResponse>>>, ST.Application.Contracts.Interfaces.Common.ICachableRequest
{
    public string CacheGroup => "module-catalogue";
    public string Discriminator => "GetModuleFlagsQuery";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
