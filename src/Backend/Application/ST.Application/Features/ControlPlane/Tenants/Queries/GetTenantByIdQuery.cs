using System;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;

namespace ST.Application.Features.ControlPlane.Tenants.Queries;

public record GetTenantByIdQuery(Guid Id) : IRequest<AppResponse<TenantResponse>>, ICachableRequest
{
    public string CacheGroup => "tenants";
    public string Discriminator => Id.ToString();
    public string? CacheUserId => null;
    public bool IsVersioned => false;
}
