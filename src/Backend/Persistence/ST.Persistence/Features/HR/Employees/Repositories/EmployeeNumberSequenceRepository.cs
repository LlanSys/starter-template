using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Persistence.Features.HR.DataContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ST.Persistence.Features.HR.Employees.Repositories;

internal sealed class EmployeeNumberSequenceRepository(HrDBContext context) : IEmployeeNumberSequenceRepository
{
    public async Task<int> AllocateNextAsync(Guid departmentId, int year, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Employee numbers must be allocated inside the employee creation transaction.");
        }

        await AcquireSequenceLockAsync(departmentId, year, cancellationToken).ConfigureAwait(false);

        var sequence = await context.EmployeeNumberSequences
            .SingleOrDefaultAsync(
                item => item.DepartmentId == departmentId && item.Year == year,
                cancellationToken)
            .ConfigureAwait(false);

        if (sequence is null)
        {
            sequence = EmployeeNumberSequence.Start(departmentId, year);
            await context.EmployeeNumberSequences.AddAsync(sequence, cancellationToken).ConfigureAwait(false);
            return sequence.LastNumber;
        }

        return sequence.Increment();
    }

    private async Task AcquireSequenceLockAsync(Guid departmentId, int year, CancellationToken cancellationToken)
    {
        var result = new SqlParameter("@result", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = $"hr-employee-number:{context.CurrentTenantId:N}:{departmentId:N}:{year}"
        };

        await context.Database.ExecuteSqlRawAsync(
                "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;",
                [result, resource],
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Value is int lockResult && lockResult >= 0)
        {
            return;
        }

        throw new TimeoutException("Could not acquire the employee number sequence lock. Please retry the operation.");
    }
}
