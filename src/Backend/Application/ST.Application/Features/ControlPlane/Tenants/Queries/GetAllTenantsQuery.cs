using System;
using System.Collections.Generic;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;

namespace ST.Application.Features.ControlPlane.Tenants.Queries;

public record GetAllTenantsQuery : IRequest<AppResponse<List<TenantResponse>>>, ICachableRequest
{
    public string CacheGroup => "tenants";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
