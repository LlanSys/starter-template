namespace ST.Application.Features.HR.Employees.Contracts.Interfaces;

public interface IEmployeeNumberGenerator
{
    Task<string> GenerateAsync(Guid departmentId, CancellationToken ct = default);
}
