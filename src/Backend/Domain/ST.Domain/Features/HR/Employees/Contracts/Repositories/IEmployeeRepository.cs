using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.HR.Employees.Contracts.Repositories;

public interface IEmployeeRepository : IRepository<Employee>
{
}
