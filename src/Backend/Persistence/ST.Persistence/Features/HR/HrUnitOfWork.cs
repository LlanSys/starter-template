using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Departments.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Common;
using ST.Persistence.Features.HR.DataContext;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.HR;

public sealed class HrUnitOfWork(
    HrDBContext context,
    IDepartmentRepository departmentRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeNumberSequenceRepository employeeNumberSequenceRepository,
    IPublisher publisher,
    ILogger<HrUnitOfWork> logger
) : BaseUnitOfWork<HrDBContext>(context, publisher, logger), IHrUnitOfWork
{
    public IDepartmentRepository DepartmentRepository { get; } = departmentRepository;
    public IEmployeeRepository EmployeeRepository { get; } = employeeRepository;
    public IEmployeeNumberSequenceRepository EmployeeNumberSequenceRepository { get; } = employeeNumberSequenceRepository;
}
