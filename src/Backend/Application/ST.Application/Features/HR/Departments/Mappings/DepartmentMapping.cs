using ST.Domain.Features.HR.Departments.Entities;
using ST.SharedKernel.Features.HR.Departments.Dtos;

namespace ST.Application.Features.HR.Departments.Mappings;

public static class DepartmentMapping
{
    public static DepartmentResponse ToDepartmentResponse(this Department department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentResponse(
            department.Id,
            department.Code,
            department.Name,
            department.Description,
            department.IsActive);
    }
}
