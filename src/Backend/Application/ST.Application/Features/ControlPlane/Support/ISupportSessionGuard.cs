using System.Security.Claims;
using ST.Domain.Features.ControlPlane.Support.Entities;
namespace ST.Application.Features.ControlPlane.Support;
public interface ISupportSessionGuard
{
 Task<SupportGrant?> ValidateAsync(ClaimsPrincipal principal,CancellationToken ct);
}
