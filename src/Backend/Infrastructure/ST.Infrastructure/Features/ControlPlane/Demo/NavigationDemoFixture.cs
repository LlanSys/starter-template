using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Demo;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Features.ControlPlane.Demo;
using ST.Domain.Features.IAM.Menus.Entities;
namespace ST.Infrastructure.Features.ControlPlane.Demo;
internal sealed class NavigationDemoFixture(IamDBContext context,ICurrentTenantProvider tenant) : IDemoFixture
{
 public int Version => 1;
 public async Task ResetAsync(CancellationToken cancellationToken) {
  if(tenant.TenantId!=DemoWorkspace.TenantId) throw new InvalidOperationException("Demo fixtures cannot target a customer workspace.");
  // The starter owns navigation fixtures only. Business modules add their own isolated fixtures here.
  context.RemoveRange(await context.NavigationOverrides.ToListAsync(cancellationToken).ConfigureAwait(false));
  context.RemoveRange(await context.Set<MenuItem>().ToListAsync(cancellationToken).ConfigureAwait(false));
  await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
 }
}
