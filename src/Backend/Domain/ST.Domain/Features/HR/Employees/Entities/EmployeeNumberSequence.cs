using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.HR.Employees.Entities;

public sealed class EmployeeNumberSequence : ISoftDeletable
{
    private EmployeeNumberSequence()
    {
    }

    private EmployeeNumberSequence(Guid departmentId, int year)
    {
        Id = Guid.CreateVersion7();
        DepartmentId = departmentId;
        Year = year;
        LastNumber = 1;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public int Year { get; private set; }
    public int LastNumber { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public static EmployeeNumberSequence Start(Guid departmentId, int year)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Department is required for employee number sequencing.", nameof(departmentId));
        }

        if (year < 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be a valid business year.");
        }

        return new EmployeeNumberSequence(departmentId, year);
    }

    public int Increment()
    {
        LastNumber++;
        return LastNumber;
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy.Trim();
    }
}
