using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using MediatR;

namespace ST.Application.Features.HR.Employees.QueryHandlers;



internal sealed class GetEmployeeByIdQueryHandler(IHrUnitOfWork unitOfWork)
    : IRequestHandler<GetEmployeeByIdQuery, AppResponse<EmployeeResponse>>
{
    public async Task<AppResponse<EmployeeResponse>> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.EmployeeRepository.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
        var departmentName = employee is null
            ? string.Empty
            : await unitOfWork.DepartmentRepository
                .FirstOrDefaultAsync(
                    departments => departments
                        .Where(department => department.Id == employee.DepartmentId)
                        .Select(static department => department.Name),
                    cancellationToken)
                .ConfigureAwait(false) ?? string.Empty;
        var managerName = employee?.ManagerId is null
            ? string.Empty
            : await unitOfWork.EmployeeRepository
                .FirstOrDefaultAsync(
                    employees => employees
                        .Where(manager => manager.Id == employee.ManagerId.Value)
                        .Select(static manager => $"{manager.FirstName} {manager.LastName}"),
                    cancellationToken)
                .ConfigureAwait(false) ?? string.Empty;

        return employee is null
            ? AppResponses.Failure<EmployeeResponse>($"Employee {query.Id} not found.")
            : AppResponses.Success("Employee loaded.", employee.ToEmployeeResponse(departmentName, managerName));
    }
}
