using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Support;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Support.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Features.ControlPlane;
namespace ST.Infrastructure.Features.ControlPlane.Support;
internal sealed class SupportSessionGuard(IControlPlaneUnitOfWork plane,UserManager<AppUser> users, TimeProvider timeProvider) : ISupportSessionGuard
{
 public async Task<SupportGrant?> ValidateAsync(ClaimsPrincipal principal,CancellationToken ct) {
  if(!Guid.TryParse(principal.FindFirstValue("support_grant_id"),out var id)) return null;
  var grant=await plane.SupportGrants.FindByIdAsync(id,ct).ConfigureAwait(false);
  var proxy=principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
  if(grant is null || !grant.IsLive(timeProvider.GetUtcNow()) || grant.ProxyUserId!=proxy || grant.VendorId!=principal.FindFirstValue("support_actor_id") || grant.WorkspaceId.ToString()!=principal.FindFirstValue("tenant_id")) return null;
  if(!await plane.Tenants.AnyAsync(x=>x.Id==grant.WorkspaceId && x.Status==TenantStatus.Active && !x.IsDemo,ct).ConfigureAwait(false)) return null;
  var actor=await users.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x=>x.Id==grant.VendorId && x.IsActive && !x.IsDeleted && x.TwoFactorEnabled,ct).ConfigureAwait(false);
  if(actor is null) return null;
  var claims=await users.GetClaimsAsync(actor).ConfigureAwait(false);
  return claims.Any(x=>x.Type==PlatformAccess.StaffClaim && x.Value=="true") && claims.Any(x=>x.Type=="permission" && x.Value==PlatformAccess.Permission) ? grant:null;
 }
}
