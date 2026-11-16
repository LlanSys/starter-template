using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.WorkTasks.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class OperationalTaskAssignmentConfiguration : IEntityTypeConfiguration<OperationalTaskAssignment>
{
    public void Configure(EntityTypeBuilder<OperationalTaskAssignment> builder)
    {
        builder.ToTable("OperationalTaskAssignments");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Kind).HasMaxLength(40).IsRequired();
        builder.Property(task => task.SubjectLabel).HasMaxLength(256).IsRequired();
        builder.Property(task => task.AssigneeUserId).HasMaxLength(128);
        builder.Property(task => task.Revision).IsConcurrencyToken();
        builder.HasIndex(task => new { task.Kind, task.SubjectId, task.CycleId, task.CycleAt }).IsUnique();
        builder.HasIndex(task => new { task.OwnerTenantId, task.AssigneeUserId, task.CompletedAt });
    }
}
