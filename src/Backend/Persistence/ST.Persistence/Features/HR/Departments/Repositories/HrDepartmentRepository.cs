using ST.Domain.Features.HR.Departments.Contracts.Repositories;
using ST.Domain.Features.HR.Departments.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.HR.DataContext;

namespace ST.Persistence.Features.HR.Departments.Repositories;

internal sealed class HrDepartmentRepository(HrDBContext context) : Repository<Department>(context), IDepartmentRepository
{
}
