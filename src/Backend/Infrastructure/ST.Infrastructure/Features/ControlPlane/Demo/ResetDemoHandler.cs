using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.ControlPlane.Demo;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Demo;
namespace ST.Infrastructure.Features.ControlPlane.Demo;
internal sealed class ResetDemoHandler(IControlPlaneUnitOfWork plane,IServiceScopeFactory scopes,ITenantWorkspaceProvisioner provisioner) : IRequestHandler<ResetDemoCommand,AppResponse<bool>>
{
 public async Task<AppResponse<bool>> Handle(ResetDemoCommand request,CancellationToken ct) {
  var demo=await plane.Tenants.FindByIdAsync(DemoWorkspace.TenantId,ct).ConfigureAwait(false);
  if(demo is null || !demo.IsDemo || demo.Identifier!=DemoWorkspace.Identifier) return AppResponses.Failure<bool>("The reserved demo workspace was not found.");
  await using var scope=scopes.CreateAsyncScope();scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId=demo.Id;
  var context=scope.ServiceProvider.GetRequiredService<IamDBContext>();var sessions=scope.ServiceProvider.GetRequiredService<ISessionService>();
  var unit=scope.ServiceProvider.GetRequiredService<IIamUnitOfWork>();
  foreach(var id in await context.Users.Select(x=>x.Id).ToListAsync(ct).ConfigureAwait(false)) {
   await sessions.RevokeAllUserSessionsAsync(id,cancellationToken:ct).ConfigureAwait(false);
   await unit.TokenRepository.RevokeAllUserTokensAsync(id,"Demo reset").ConfigureAwait(false);
  }
  await unit.CompleteAsync(ct).ConfigureAwait(false);
  var fixtures=scope.ServiceProvider.GetServices<IDemoFixture>().ToArray();
  foreach(var fixture in fixtures) await fixture.ResetAsync(ct).ConfigureAwait(false);
  demo.DemoFixtureVersion=fixtures.Select(x=>x.Version).DefaultIfEmpty(1).Max();demo.DemoResetAt=DateTimeOffset.UtcNow;demo.UpdatedBy=request.ActorId;
  await plane.CompleteAsync(ct).ConfigureAwait(false);
  var prepared=await provisioner.InitializeAsync(demo.Id,null,request.ActorId,ct).ConfigureAwait(false);
  return prepared.IsSuccess ? AppResponses.Success(true) : AppResponses.Failure<bool>(prepared.Message ?? "Demo setup failed.");
 }
}
