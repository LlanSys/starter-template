using ST.Application.Features.HR.Employees.Contracts.Interfaces;
using ST.Domain.Features.HR.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.HR.Employees.Contracts.Implementations;


internal sealed class EmployeeNumberGenerator(IHrUnitOfWork _hrUnitOfWork) : IEmployeeNumberGenerator
{
    public async Task<string> GenerateAsync(Guid departmentId, CancellationToken ct = default)
    {
        var department = await _hrUnitOfWork.DepartmentRepository.FindByIdAsync(departmentId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Department is required before generating an employee number.");

        var prefix = department.Code;
        var year = DateTime.UtcNow.Year;
        var sequence = await _hrUnitOfWork.EmployeeNumberSequenceRepository
            .AllocateNextAsync(departmentId, year, ct)
            .ConfigureAwait(false);

        return $"{prefix}-{year}-{sequence:D4}";
    }
}
