using ST.Domain.Features.HR.Employees.Entities;
using ST.Domain.Features.HR.Departments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.HR.Employees.EntityConfigurations;

internal sealed class EmployeeNumberSequenceConfiguration : IEntityTypeConfiguration<EmployeeNumberSequence>
{
    public void Configure(EntityTypeBuilder<EmployeeNumberSequence> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EmployeeNumberSequences");

        builder.HasKey(sequence => sequence.Id);

        builder.Property(sequence => sequence.TenantId)
            .IsRequired();

        builder.Property(sequence => sequence.DepartmentId)
            .IsRequired();

        builder.Property(sequence => sequence.Year)
            .IsRequired();

        builder.Property(sequence => sequence.LastNumber)
            .IsRequired();

        builder.Property(sequence => sequence.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sequence => sequence.DeletedAt);

        builder.Property(sequence => sequence.DeletedBy)
            .HasMaxLength(100);

        builder.Property(sequence => sequence.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(sequence => new { sequence.TenantId, sequence.DepartmentId, sequence.Year })
            .IsUnique()
            .HasDatabaseName("UX_EmployeeNumberSequences_TenantId_DepartmentId_Year");

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(sequence => sequence.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
