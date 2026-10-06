using ST.Domain.Shared.Contracts;
using ST.Domain.Features.HR.Departments.Contracts.Repositories;
using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.HR.Contracts;

public interface IHrUnitOfWork : ITransactionalUnitOfWork
{
    IDepartmentRepository DepartmentRepository { get; }
    IEmployeeRepository EmployeeRepository { get; }
    IEmployeeNumberSequenceRepository EmployeeNumberSequenceRepository { get; }
}
