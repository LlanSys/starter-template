using ST.Domain.Features.Shared.Payments.Contracts.Repositories;
using ST.Domain.Features.Shared.Payments.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.Shared.DataContext;

namespace ST.Persistence.Features.Shared.Payments.Repositories;

internal sealed class SharedPaymentRecordRepository(SharedDBContext context) : Repository<PaymentRecord>(context), IPaymentRecordRepository;
