using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Application.Features.ControlPlane.Tenants.Queries;

public sealed record GetTenantFrontendAddressQuery(Guid Id) : IRequest<AppResponse<TenantFrontendAddressResponse>>, ICachableRequest
{
    public string CacheGroup => "tenants";
    public string Discriminator => "frontend-address:" + Id;
    public string? CacheUserId => null;
    public bool BypassCache => true;
    public bool IsVersioned => false;
}
internal sealed class GetTenantFrontendAddressHandler(IControlPlaneUnitOfWork unit) : IRequestHandler<GetTenantFrontendAddressQuery, AppResponse<TenantFrontendAddressResponse>>
{
    public async Task<AppResponse<TenantFrontendAddressResponse>> Handle(GetTenantFrontendAddressQuery request, CancellationToken ct)
    {
        var tenant = await unit.Tenants.FindByIdAsync(request.Id, ct).ConfigureAwait(false);
        return tenant is null ? AppResponses.NotFound<TenantFrontendAddressResponse>("Tenant not found.")
            : AppResponses.Success(new TenantFrontendAddressResponse(tenant.DedicatedFrontendOrigin, Convert.ToBase64String(tenant.RowVersion)));
    }
}
