using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Common;

public abstract class BaseUnitOfWork<TContext>(
    TContext context,
    IPublisher publisher,
    ILogger logger,
    TimeProvider timeProvider
) where TContext : DbContext
{
    protected TContext Context { get; } = context;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger _logger = logger;

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken)
    {
        var strategy = Context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var transaction = await Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredTransaction = transaction.ConfigureAwait(false);
            try
            {
                var result = await operation().ConfigureAwait(false);
                await DispatchDomainEventsAsync(cancellationToken).ConfigureAwait(false);
                await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                PersistenceLogDefinitions.LogTransactionConcurrencyRollback(_logger, ex);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                Context.ChangeTracker.Clear();
                throw;
            }
            catch (Exception ex)
            {
                PersistenceLogDefinitions.LogTransactionErrorRollback(_logger, ex);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    public async Task<TResult> ExecuteInTransactionWithRetryAsync<TResult>(Func<Task<TResult>> operation, int maxRetries = 3, int baseDelayMs = 50)
    {
        var strategy = Context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                var transaction = await Context.Database.BeginTransactionAsync().ConfigureAwait(false);
                await using var configuredTransaction = transaction.ConfigureAwait(false);
                try
                {
                    var result = await operation().ConfigureAwait(false);
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                    await transaction.CommitAsync().ConfigureAwait(false);
                    return result;
                }
                catch (DbUpdateConcurrencyException ex) when (attempt < maxRetries)
                {
                    PersistenceLogDefinitions.LogTransactionConcurrencyRetry(_logger, attempt, maxRetries, ex);
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    Context.ChangeTracker.Clear();
                    await Task.Delay(TimeSpan.FromMilliseconds(baseDelayMs * attempt), timeProvider).ConfigureAwait(false);
                    continue;
                }
                catch (Exception ex)
                {
                    PersistenceLogDefinitions.LogRetryableTransactionErrorRollback(_logger, ex);
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    throw;
                }
            }
            throw new InvalidOperationException("Max retry attempts exceeded due to concurrency conflicts.");
        }).ConfigureAwait(false);
    }

    public async Task<int> CompleteAsync(CancellationToken ct = default)
        => await Context.SaveChangesAsync(ct).ConfigureAwait(false);

    public IReadOnlyList<IDomainEvent> GetPendingDomainEvents()
    {
        var domainEntities = Context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        return domainEntities.SelectMany(x => x.DomainEvents).ToList().AsReadOnly();
    }

    public void ClearDomainEvents()
    {
        var domainEntities = Context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        domainEntities.ForEach(x => x.ClearDomainEvents());
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = Context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities.SelectMany(x => x.DomainEvents).ToList();
        domainEntities.ForEach(x => x.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
    }
}
