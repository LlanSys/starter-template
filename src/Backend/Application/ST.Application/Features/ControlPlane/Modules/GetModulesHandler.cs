using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class GetModulesHandler(IControlPlaneUnitOfWork unitOfWork) : IRequestHandler<GetModulesQuery, AppResponse<IReadOnlyList<ModuleResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<ModuleResponse>>> Handle(GetModulesQuery request, CancellationToken cancellationToken)
    {
        var modules = await unitOfWork.Modules.ListAsync(query => query.OrderBy(module => module.Name), cancellationToken).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<ModuleResponse>>(modules.Select(module => new ModuleResponse(module.Id, module.Key, module.Name, module.Description, module.IsActive, ModuleImplementations.Contains(module.Key)) { IsRetired = module.IsRetired, DependencyKeys = module.DependencyKeys }).ToList());
    }
}
