using ST.Application.Contracts.Interfaces.Common;
using System.Collections.Generic;
using System; using ST.SharedKernel.Dtos.Common; using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos; using MediatR;  namespace ST.Application.Features.ControlPlane.Tenants.Commands;  public record ActivateTenantCommand(Guid Id) : IRequest<AppResponse<TenantResponse>>, ICacheInvalidatorRequest
{
    public IEnumerable<string> CacheGroups => ["tenants"];
    public string? CacheUserId => null;
}
