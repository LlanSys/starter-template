using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ST.Api.Features.Shared.Realtime;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}").ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}
