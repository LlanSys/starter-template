using ST.Domain.Features.HR.Employees.Entities;
using ST.Domain.Shared.Entities;

namespace ST.Persistence.Features.HR.Employees.Seeds;

/// <summary>
/// Provides deterministic seed data for the <see cref="Employee"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// All <see cref="BaseEntity.Id"/> values are pre-generated, fixed GUIDs (Version 7 format
/// for visual consistency with runtime IDs) and <see cref="BaseEntity.CreatedAt"/> is a
/// fixed UTC timestamp. This is a hard requirement for EF Core <c>HasData</c> seeding:
/// non-deterministic values (e.g. <c>Guid.CreateVersion7()</c>, <c>DateTimeOffset.UtcNow</c>)
/// cause EF to detect a model change on every <c>dotnet ef migrations add</c> run, even
/// when no real schema change has occurred, polluting migration history.
/// </para>
/// <para>
/// When adding new seed employees, generate a new Version 7 GUID once (e.g. via
/// <c>Guid.CreateVersion7()</c> in a scratch project or <c>dotnet-script</c>) and paste
/// the value as a literal here.
/// </para>
/// </remarks>
public static class EmployeeSeed
{
    // Fixed seed timestamp — shared by all seed rows for consistency.
    // Must never be changed once the initial migration has been applied.
    private static readonly Guid BaseTenantId = new("0194f700-0000-7000-8000-000000000001");
    private static readonly DateTimeOffset SeedDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // --- Department IDs (Deterministic V7 GUIDs) ---
    private static readonly Guid IdDeptHr = new("0194f800-0000-7000-8000-000000000100");
    private static readonly Guid IdDeptFinance = new("0194f800-0000-7000-8000-000000000200");
    private static readonly Guid IdDeptIt = new("0194f800-0000-7000-8000-000000000300");
    // --- Employee IDs (Deterministic V7 GUIDs) ---
    private static readonly Guid IdHrManager = new("0194f800-0000-7000-8000-000000000001");
    private static readonly Guid IdFinanceClerk = new("0194f800-0000-7000-8000-000000000002");
    private static readonly Guid IdItAdmin = new("0194f800-0000-7000-8000-000000000003");

    private static readonly (Guid Id, string Number, string Email, string FirstName, string LastName, string IdNumber, string PhoneNationalNumber, Guid DepartmentId, Guid ManagerId)[] Blueprints =
    [
        (IdHrManager, "EMP-001", "aamodhiambo@gmail.com", "Alex", "Odhiambo", "14042262", "798980115", IdDeptHr, Guid.Empty),
        (IdFinanceClerk, "EMP-002", "allan.alex0803@gmail.com", "Allan", "Alex", "67532424", "700057578", IdDeptFinance, IdHrManager),
        (IdItAdmin, "EMP-003", "omitolaura469@gmail.com", "Laura", "Omito", "76945774", "719423686", IdDeptIt, IdHrManager)
    ];

    public static ICollection<Employee> GetSeedData()
    {
        return [.. Blueprints.Select(bp =>
        {
            return CreateSeedEmployee(
                bp.Id,
                bp.Number,
                bp.Email,
                bp.FirstName,
                bp.LastName,
                bp.IdNumber,
                "+254",
                bp.PhoneNationalNumber,
                $"+254{bp.PhoneNationalNumber}",
                bp.DepartmentId,
                bp.ManagerId,
                "System",
                SeedDate);
        })];
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a seed <see cref="Employee"/> using the domain factory, then overwrites
    /// the auto-generated <see cref="BaseEntity.Id"/> and <see cref="BaseEntity.CreatedAt"/>
    /// with deterministic values safe for EF Core migrations.
    /// </summary>
    private static Employee CreateSeedEmployee(
        Guid id,
        string number,
        string email,
        string firstName,
        string lastName,
        string idNumber,
        string countryCode,
        string phoneNationalNumber,
        string phoneNumber,
        Guid departmentId,
        Guid managerId,
        string createdBy,
        DateTimeOffset createdAt

    )
    {
        // Go through the domain factory so domain defaults and any future
        // constructor-time invariants are applied consistently.
        var employee = Employee.Create(
            number,
            email,
            firstName,
            lastName,
            idNumber,
            countryCode,
            phoneNationalNumber,
            phoneNumber,
            departmentId,
            managerId,
            createdBy

        );

        // Overwrite with deterministic values for migration stability.
        // BaseEntity exposes public setters on Id and CreatedAt specifically to
        // support EF Core materialisation; using them here is intentional.
        employee.Id = id;
        employee.TenantId = BaseTenantId;
        employee.CreatedAt = createdAt;

        return employee;
    }
}
