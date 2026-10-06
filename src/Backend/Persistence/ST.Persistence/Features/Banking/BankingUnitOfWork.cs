using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Common;
using ST.Persistence.Features.Banking.DataContext;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.Banking;

public sealed class BankingUnitOfWork(
    BankingDBContext context,
    ICustomerRepository customerRepository,
    IPublisher publisher,
    ILogger<BankingUnitOfWork> logger
) : BaseUnitOfWork<BankingDBContext>(context, publisher, logger), IBankingUnitOfWork
{
    public ICustomerRepository CustomerRepository { get; } = customerRepository;
}
