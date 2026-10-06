using ST.Application.Contracts.Interfaces.Common;
using System.Collections.Generic;
using ST.SharedKernel.Dtos.Common; using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos; using MediatR;  using ST.Application.Contracts.Interfaces.Common; using System.Collections.Generic;  namespace ST.Application.Features.ControlPlane.Tenants.Commands;  public record CreateTenantCommand(CreateTenantRequest Request) : IRequest<AppResponse<TenantResponse>>, ICacheInvalidatorRequest {     public IEnumerable<string> CacheGroups => ["tenants"];     public string? CacheUserId => null; }
