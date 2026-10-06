using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;

namespace ST.UI.Rcl.Features.HR.Employees.Contracts.Interfaces;

public interface IEmployeeService
{
    Task<AppResponse<PagedResponse<EmployeeResponse, Guid>>> SearchAsync(EmployeeSearchRequest request);

    Task<AppResponse<EmployeeResponse>> GetByIdAsync(Guid id);

    Task<AppResponse<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request);

    Task<AppResponse<EmployeeResponse>> UpdateAsync(Guid id, UpdateEmployeeRequest request);

    Task<AppResponse<bool>> DeleteAsync(Guid id);
}
