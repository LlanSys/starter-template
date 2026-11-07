using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Support;
using ST.Application.Contracts.Interfaces.Common;
namespace ST.Application.Features.ControlPlane.Support;
public sealed record GetSupportAuditQuery(Guid GrantId, bool Platform = false) : IRequest<AppResponse<IReadOnlyList<SupportAuditResponse>>>, ICachableRequest
{
 public string CacheGroup => "support";
 public string Discriminator => "GetSupportAuditQuery";
 public string? CacheUserId => null;
 public bool IsVersioned => false;
 public bool BypassCache => true;
}
