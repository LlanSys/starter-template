using ST.Domain.Features.HR.Departments.Entities;

namespace ST.Persistence.Features.HR.Departments.Seeds;

internal static class DepartmentSeed
{
    private static readonly Guid BaseTenantId = new("0194f700-0000-7000-8000-000000000001");
    private static readonly DateTimeOffset SeedDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyCollection<Department> GetSeedData()
    {
        return
        [
            Create(new Guid("0194f800-0000-7000-8000-000000000100"), "HR", "Human Resources", "People operations, employee lifecycle, and culture."),
            Create(new Guid("0194f800-0000-7000-8000-000000000200"), "FIN", "Finance", "Finance operations, reporting, controls, and treasury."),
            Create(new Guid("0194f800-0000-7000-8000-000000000300"), "IT", "Information Technology", "Platforms, systems administration, security, and support."),
            Create(new Guid("0194f800-0000-7000-8000-000000000400"), "LEGAL", "Legal", "Legal advisory, compliance, contracts, and governance."),
            Create(new Guid("0194f800-0000-7000-8000-000000000500"), "OPS", "Operations", "Day-to-day operations, service delivery, and process excellence.")
        ];
    }

    private static Department Create(Guid id, string code, string name, string description)
    {
        var department = Department.Create(code, name, description, "System");
        department.Id = id;
        department.TenantId = BaseTenantId;
        department.CreatedAt = SeedDate;
        return department;
    }
}
