using MassTransit;
using MassTransit.Middleware.Outbox;
using Microsoft.Extensions.Options;

namespace ST.Api.Common.Messaging;

internal sealed class OutboxDeliveryNotification(
    IOptions<OutboxDeliveryServiceOptions> options,
    TimeProvider timeProvider) : IBusOutboxNotification
{
    private TaskCompletionSource _delivery = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task WaitForDelivery(CancellationToken cancellationToken)
    {
        var pending = Volatile.Read(ref _delivery).Task;

        try
        {
            await pending.WaitAsync(options.Value.QueryDelay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Each worker polls its own database when the signal interval elapses.
        }
    }

    public void Delivered()
    {
        var next = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _delivery, next).TrySetResult();
    }
}
