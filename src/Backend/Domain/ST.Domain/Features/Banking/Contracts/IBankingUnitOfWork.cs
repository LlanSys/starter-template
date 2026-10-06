using ST.Domain.Shared.Contracts;
using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.Banking.Contracts;

public interface IBankingUnitOfWork : ITransactionalUnitOfWork
{
    ICustomerRepository CustomerRepository { get; }
}
