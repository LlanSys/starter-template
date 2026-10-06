using ST.Application.Contracts.Interfaces.Common;
using MediatR;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class NoOpBackgroundJobService : IBackgroundJobService
{
    public void Enqueue(IRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
    }

    public Task EnqueueAsync(IRequest? request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.CompletedTask;
    }
}
