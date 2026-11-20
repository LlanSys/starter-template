using MassTransit;
using Microsoft.Extensions.Options;
using ST.Api.Common.Messaging;

namespace ST.Tests.Unit;

public sealed class OutboxDeliveryNotificationTests
{
    [Fact]
    public async Task SignalReleasesBothWorkersAndOneCancellationLeavesTheOthersWaiting()
    {
        var notification = new OutboxDeliveryNotification(
            Options.Create(new OutboxDeliveryServiceOptions { QueryDelay = TimeSpan.FromMinutes(1) }),
            TimeProvider.System);

        using var cancellation = new CancellationTokenSource();
        var stopping = notification.WaitForDelivery(cancellation.Token);
        var iam = notification.WaitForDelivery(CancellationToken.None);
        var shared = notification.WaitForDelivery(CancellationToken.None);

        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopping);
        Assert.False(iam.IsCompleted);
        Assert.False(shared.IsCompleted);

        notification.Delivered();

        await Task.WhenAll(iam, shared).WaitAsync(TimeSpan.FromSeconds(5));
    }
}
