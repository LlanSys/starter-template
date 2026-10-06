using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.Banking.DataContext;

namespace ST.Persistence.Features.Banking.Customers.Repositories;

internal sealed class BankingCustomerRepository(BankingDBContext context) : Repository<Customer>(context), ICustomerRepository { }
