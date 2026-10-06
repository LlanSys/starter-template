using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.Shared.EmailTemplates.Contracts.Repositories;
using ST.Domain.Features.Shared.FailedMessages.Contracts.Repositories;

using ST.Domain.Features.Shared.Payments.Contracts.Repositories;
using ST.Domain.Features.Shared.OrgSettings.Contracts.Repositories;

namespace ST.Domain.Features.Shared.Contracts;

public interface ISharedUnitOfWork : ITransactionalUnitOfWork
{

    IEmailTemplateRepository EmailTemplateRepository { get; }
    IFailedMessageRepository FailedMessageRepository { get; }
    IPaymentRecordRepository PaymentRecordRepository { get; }
    IOrgSettingRepository OrgSettingRepository { get; }

    Task<int> CompleteWithEventsAsync(List<IIntegrationEvent>? appEvents = null, CancellationToken ct = default);
}
