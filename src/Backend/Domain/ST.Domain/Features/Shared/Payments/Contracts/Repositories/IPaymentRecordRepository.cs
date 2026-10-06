using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.Shared.Payments.Entities;

namespace ST.Domain.Features.Shared.Payments.Contracts.Repositories;

public interface IPaymentRecordRepository : IRepository<PaymentRecord>;
