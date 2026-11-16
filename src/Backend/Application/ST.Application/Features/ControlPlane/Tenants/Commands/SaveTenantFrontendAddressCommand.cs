using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public sealed record SaveTenantFrontendAddressCommand(Guid Id, SaveTenantFrontendAddressRequest Request) : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("tenants")];
}
