namespace ST.Domain.Features.HR.Employees.Contracts.Repositories;

public interface IEmployeeNumberSequenceRepository
{
    Task<int> AllocateNextAsync(Guid departmentId, int year, CancellationToken cancellationToken);
}
