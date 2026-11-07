using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using ST.Application.Features.ControlPlane.Support;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Support.Entities;
namespace ST.Api.Middleware;
internal sealed class SupportSessionMiddleware(RequestDelegate next)
{
 public async Task InvokeAsync(HttpContext context,ISupportSessionGuard guard,IControlPlaneUnitOfWork plane) {
  if(!context.User.HasClaim(x=>x.Type=="support_grant_id")) { await next(context).ConfigureAwait(false);return; }
  var grant=await guard.ValidateAsync(context.User,context.RequestAborted).ConfigureAwait(false);
  if(grant is null) { await Deny(context,"This support session expired or was revoked.").ConfigureAwait(false);return; }
  var action=context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
  var lifecycle=action?.ControllerName=="Auth" && action.ActionName is "Logout" or "RefreshToken"
    || action?.ControllerName=="TenantSupport" && action.ActionName=="Exit" && Guid.TryParse(context.Request.RouteValues["id"]?.ToString(),out var exitId) && exitId==grant.Id;
  var accountRead=HttpMethods.IsGet(context.Request.Method) && (action?.ControllerName=="Auth" && action.ActionName=="GetCurrentUser"
    || action?.ControllerName=="IamAdmin" && action.ActionName=="GetNavigationMenus");
  var policies=context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>().Where(x=>x.Policy?.StartsWith("Permission:",StringComparison.Ordinal)==true).Select(x=>x.Policy![11..]).ToArray() ?? [];
  if(!lifecycle && !accountRead && (policies.Length==0 || policies.Any(x=>!grant.PermissionKeys.Contains(x,StringComparer.OrdinalIgnoreCase)))) { await Deny(context,"This action is outside the approved support scope.").ConfigureAwait(false);return; }
  if(grant.ReadOnly && !HttpMethods.IsGet(context.Request.Method) && !lifecycle) { await Deny(context,"This support session is read-only.").ConfigureAwait(false);return; }
  var claims=context.User.Claims.Where(x=>x.Type!=ClaimTypes.Role && x.Type!="platform_staff" && (x.Type!="permission" || grant.PermissionKeys.Contains(x.Value,StringComparer.OrdinalIgnoreCase))).ToArray();
  context.User=new ClaimsPrincipal(new ClaimsIdentity(claims,context.User.Identity!.AuthenticationType,ClaimTypes.Name,ClaimTypes.Role));
  context.Items["TrustedWorkspaceProxy"]=true;
  // Persist intent before dispatch. If audit storage fails, no customer action runs.
  var audit=new SupportAudit { GrantId=grant.Id,WorkspaceId=grant.WorkspaceId,ActorId=grant.VendorId,Action=$"{context.Request.Method} {context.GetEndpoint()?.DisplayName ?? "request"}" };
  if(audit.Action.Length>400) audit.Action=audit.Action[..400];
  await plane.SupportAudit.CreateAsync(audit,context.RequestAborted).ConfigureAwait(false);await plane.CompleteAsync(context.RequestAborted).ConfigureAwait(false);
  await next(context).ConfigureAwait(false);
  audit.StatusCode=context.Response.StatusCode;await plane.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
 }
 private static async Task Deny(HttpContext context,string message) { context.Response.StatusCode=403;await context.Response.WriteAsJsonAsync(new{message},context.RequestAborted).ConfigureAwait(false); }
}
