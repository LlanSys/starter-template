using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.HR.DataContext;

namespace ST.Persistence.Features.HR.Employees.Repositories;

internal sealed class HrEmployeeRepository(HrDBContext context) : Repository<Employee>(context), IEmployeeRepository { }
