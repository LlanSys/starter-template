using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.ControlPlane.Support;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Support.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Support;
using ST.SharedKernel.Features.ControlPlane.Demo;
using ST.SharedKernel.Features.IAM.Users.Dtos;
namespace ST.Infrastructure.Features.ControlPlane.Support;
internal sealed class SupportHandler(IControlPlaneUnitOfWork plane,ICurrentTenantProvider tenant,ICurrentActorProvider actor,IHttpContextAccessor http,IServiceScopeFactory scopes,UserManager<AppUser> sourceUsers,ISupportSessionGuard guard, TimeProvider timeProvider) : IRequestHandler<SupportCommand,AppResponse<SupportResult>>
{
 public async Task<AppResponse<SupportResult>> Handle(SupportCommand command,CancellationToken ct) {
  var now = timeProvider.GetUtcNow();
  var principal=http.HttpContext!.User;var staff=PlatformAccess.IsStaff(principal);SupportGrant? grant;
  if(command.Action=="request") {
   if(!staff || principal.HasClaim(x=>x.Type=="support_grant_id")) return Denied();
   var request=command.Request!;
   var target=await plane.Tenants.FindByIdAsync(request.TenantId,ct).ConfigureAwait(false);
   if(target is null || target.IsDemo || target.Id==Guid.Parse("0194f700-0000-7000-8000-000000000001") || target.Status!=TenantStatus.Active) return AppResponses.Failure<SupportResult>("Choose an active customer workspace.");
   var vendor=await sourceUsers.FindByIdAsync(actor.ActorId).ConfigureAwait(false);
   if(vendor?.TwoFactorEnabled!=true || !vendor.IsActive || vendor.IsDeleted || !PlatformAccess.HasMfa(principal)) return AppResponses.Failure<SupportResult>("Complete platform administrator MFA first.");
   var authority=await sourceUsers.GetClaimsAsync(vendor).ConfigureAwait(false);
   if(!authority.Any(x=>x.Type==PlatformAccess.StaffClaim && x.Value=="true") || !authority.Any(x=>x.Type=="permission" && x.Value==PlatformAccess.Permission)) return Denied();
   await using var scope=scopes.CreateAsyncScope();scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId=target.Id;
   var access=await scope.ServiceProvider.GetRequiredService<IEffectiveModulePolicy>().GetAsync(ct).ConfigureAwait(false);
   var keys=request.PermissionKeys.Select(x=>x.Trim().ToLowerInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
   if(keys.Any(x=>!SafePermission(x) || !access.PermissionAvailable(x)) || keys.Length==0) return AppResponses.Failure<SupportResult>("Select permissions available in the customer workspace.");
   grant=new SupportGrant { WorkspaceId=target.Id,VendorId=actor.ActorId,Reason=request.Reason.Trim(),PermissionKeys=keys,ReadOnly=request.ReadOnly,DurationMinutes=request.DurationMinutes,CreatedBy=actor.ActorId };
   await plane.SupportGrants.CreateAsync(grant,ct).ConfigureAwait(false);
  } else {
   grant=await plane.SupportGrants.FindByIdAsync(command.GrantId!.Value,ct).ConfigureAwait(false);
   if(grant is null) return Denied();
   if(command.Action=="approve") {
    if(staff || principal.HasClaim(x=>x.Type=="support_grant_id") || grant.WorkspaceId!=tenant.TenantId || grant.VendorId==actor.ActorId || grant.Status!="Pending" || grant.CreatedAt<now.AddDays(-1)) return Denied();
    if(!await plane.Tenants.AnyAsync(x=>x.Id==grant.WorkspaceId && x.Status==TenantStatus.Active,ct).ConfigureAwait(false)) return Denied();
    var access=await http.HttpContext.RequestServices.GetRequiredService<IEffectiveModulePolicy>().GetAsync(ct).ConfigureAwait(false);
    var approval=command.Request;
    var keys=(approval?.PermissionKeys ?? grant.PermissionKeys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    var minutes=approval?.DurationMinutes ?? grant.DurationMinutes;
    if(keys.Length==0 || keys.Any(x=>!grant.PermissionKeys.Contains(x,StringComparer.OrdinalIgnoreCase) || !access.PermissionAvailable(x) || !SafePermission(x)) || minutes<5 || minutes>grant.DurationMinutes || (grant.ReadOnly && approval?.ReadOnly==false)) return AppResponses.Failure<SupportResult>("Approval can only narrow the requested permissions, time and write access.");
    grant.PermissionKeys=keys;grant.DurationMinutes=minutes;grant.ReadOnly=approval?.ReadOnly ?? grant.ReadOnly;grant.ApprovedBy=actor.ActorId;grant.ExpiresAt=now.AddMinutes(minutes);grant.Status="Approved";
   } else if(command.Action=="revoke") {
    if(principal.HasClaim(x=>x.Type=="support_grant_id") || (!staff && grant.WorkspaceId!=tenant.TenantId)) return Denied();
    grant.Status="Revoked";
   } else if(command.Action=="exit") {
    var live=await guard.ValidateAsync(principal,ct).ConfigureAwait(false);
    if(live?.Id!=grant.Id) return Denied();
    grant.Status="Ended";
   } else if(command.Action=="enter") {
    if(!staff || grant.VendorId!=actor.ActorId || !grant.IsLive(now) || !PlatformAccess.HasMfa(principal)) return Denied();
    return await EnterAsync(grant,now, ct).ConfigureAwait(false);
   }
  }
  grant.UpdatedBy=actor.ActorId;grant.UpdatedAt=now;
  await AuditAsync(grant,command.Action,now, ct).ConfigureAwait(false);
  await plane.CompleteAsync(ct).ConfigureAwait(false);
  return AppResponses.Success(new SupportResult(SupportMapping.ToResponse(grant)));
 }
 private async Task<AppResponse<SupportResult>> EnterAsync(SupportGrant grant, DateTimeOffset now, CancellationToken ct) {
  var vendor=await sourceUsers.FindByIdAsync(actor.ActorId).ConfigureAwait(false);
  var target=await plane.Tenants.FindByIdAsync(grant.WorkspaceId,ct).ConfigureAwait(false);
  if(vendor?.TwoFactorEnabled!=true || !vendor.IsActive || vendor.IsDeleted || target?.Status!=TenantStatus.Active) return Denied();
  var authority=await sourceUsers.GetClaimsAsync(vendor).ConfigureAwait(false);
  if(!authority.Any(x=>x.Type==PlatformAccess.StaffClaim && x.Value=="true") || !authority.Any(x=>x.Type=="permission" && x.Value==PlatformAccess.Permission)) return Denied();
  await using var scope=scopes.CreateAsyncScope();scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId=grant.WorkspaceId;
  var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var unit=scope.ServiceProvider.GetRequiredService<IIamUnitOfWork>();
  var name=$"support-{grant.Id:N}";var user=await users.FindByNameAsync(name).ConfigureAwait(false);
  if(user is null) { user=AppUser.CreateSystemUser(now, grant.WorkspaceId,name,$"{name}@example.invalid",grant.VendorId);user.FirstName=vendor.FirstName;user.LastName="(Support)";user.IsActive=true;user.EmailConfirmed=true;Ensure(await users.CreateAsync(user).ConfigureAwait(false)); }
  if(user.TenantId!=grant.WorkspaceId || !user.IsActive || user.IsDeleted) return Denied();
  var existing=await users.GetClaimsAsync(user).ConfigureAwait(false);
  if(existing.Count>0) Ensure(await users.RemoveClaimsAsync(user,existing).ConfigureAwait(false));
  Ensure(await users.AddClaimsAsync(user,grant.PermissionKeys.Select(x=>new Claim("permission",x)).Append(new Claim("support_grant_id",grant.Id.ToString())).Append(new Claim("support_actor_id",grant.VendorId))).ConfigureAwait(false));
  grant.ProxyUserId=user.Id;grant.UpdatedBy=grant.VendorId;grant.UpdatedAt=now;
  await AuditAsync(grant,"enter",now, ct).ConfigureAwait(false);await plane.CompleteAsync(ct).ConfigureAwait(false);
  var sessions=scope.ServiceProvider.GetRequiredService<ISessionService>();
  var session=await sessions.CreateSessionAsync(user.Id,Guid.CreateVersion7(),http.HttpContext!.Connection.RemoteIpAddress?.ToString() ?? "unknown","Managed tenant support",ct).ConfigureAwait(false);
  if(!session.IsSuccess) return AppResponses.Failure<SupportResult>("Could not establish a support session.");
  var claims=await scope.ServiceProvider.GetRequiredService<IClaimsService>().GetUserClaimsAsync(user,session.Data).ConfigureAwait(false);
  var jwt=scope.ServiceProvider.GetRequiredService<IJwtService>();var token=await jwt.CreateTokenAsync(claims).ConfigureAwait(false);var refresh=jwt.CreateRefreshToken();
  await unit.TokenRepository.AddRefreshTokenAsync(RefreshToken.Create(now, user.Id,refresh,grant.ExpiresAt!.Value,grant.VendorId,null)).ConfigureAwait(false);await unit.CompleteAsync(ct).ConfigureAwait(false);
  return AppResponses.Success(new SupportResult(SupportMapping.ToResponse(grant),new LoginResponse(user.Id,user.FirstName,user.LastName,user.Email!,false,false,true,token,refresh,session.Data.ToString(),jwt.GetTokenExpiry(token),user.ToAppUserResponse(),null)));
 }
 private async Task AuditAsync(SupportGrant grant, string action, DateTimeOffset now, CancellationToken ct)=>await plane.SupportAudit.CreateAsync(new SupportAudit { CreatedAt = now, GrantId=grant.Id,WorkspaceId=grant.WorkspaceId,ActorId=actor.ActorId,Action=action },ct).ConfigureAwait(false);
 private static bool SafePermission(string x)=> !x.StartsWith("control_plane.",StringComparison.OrdinalIgnoreCase) && !x.StartsWith("support.",StringComparison.OrdinalIgnoreCase);
 private static AppResponse<SupportResult> Denied()=>AppResponses.Failure<SupportResult>("This support request is not available in your current context.");
 private static void Ensure(IdentityResult result) { if(!result.Succeeded) throw new InvalidOperationException("Support identity setup failed."); }
}
