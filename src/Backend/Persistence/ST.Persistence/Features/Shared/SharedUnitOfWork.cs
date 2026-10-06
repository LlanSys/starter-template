using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Features.Shared.Payments.Contracts.Repositories;
using ST.Domain.Features.Shared.OrgSettings.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Common;
using ST.Persistence.Logging;
using ST.Persistence.Features.Shared.DataContext;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.Shared;

public sealed class SharedUnitOfWork(
    SharedDBContext context,
    IEmailTemplateRepository emailTemplateRepository,
    IFailedMessageRepository failedMessageRepository,
    IPaymentRecordRepository paymentRecordRepository,
    IOrgSettingRepository OrgSettingRepository,
    IPublisher publisher,
    ILogger<SharedUnitOfWork> logger
) : BaseUnitOfWork<SharedDBContext>(context, publisher, logger), ISharedUnitOfWork
{
    private readonly SharedDBContext _sharedContext = context;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<SharedUnitOfWork> _logger = logger;

    public IEmailTemplateRepository EmailTemplateRepository { get; } = emailTemplateRepository;
    public IFailedMessageRepository FailedMessageRepository { get; } = failedMessageRepository;
    public IPaymentRecordRepository PaymentRecordRepository { get; } = paymentRecordRepository;
    public IOrgSettingRepository OrgSettingRepository { get; } = OrgSettingRepository;

    public async Task<int> CompleteWithEventsAsync(List<IIntegrationEvent>? appEvents = null, CancellationToken ct = default)
    {
        var transaction = await _sharedContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using var configuredTransaction = transaction.ConfigureAwait(false);

        try
        {
            var result = await _sharedContext.SaveChangesAsync(ct).ConfigureAwait(false);
            var domainEvents = _sharedContext.GetCollectedDomainEvents() ?? Array.Empty<IDomainEvent>();

            foreach (var domainEvent in domainEvents)
                await _publisher.Publish(domainEvent, ct).ConfigureAwait(false);

            if (appEvents != null)
                foreach (var appEvent in appEvents)
                    await _publisher.Publish(appEvent, ct).ConfigureAwait(false);

            await transaction.CommitAsync(ct).ConfigureAwait(false);
            _sharedContext.ClearCollectedDomainEvents();

            PersistenceLogDefinitions.LogEventsPublished(_logger, domainEvents.Count);
            return result;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogCompleteWithEventsRollback(_logger, ex);
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            _sharedContext.ClearCollectedDomainEvents();
            throw;
        }
    }
}
