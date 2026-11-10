using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Support;
using ST.Application.Contracts.Interfaces.Common;
namespace ST.Application.Features.ControlPlane.Support;
public sealed record GetSupportGrantsQuery(bool Platform = false) : IRequest<AppResponse<IReadOnlyList<SupportGrantResponse>>>, ICachableRequest
{
 public string CacheGroup => "support";
 public string Discriminator => "GetSupportGrantsQuery";
 public string? CacheUserId => null;
 public bool IsVersioned => false;
 public bool BypassCache => true;
}
