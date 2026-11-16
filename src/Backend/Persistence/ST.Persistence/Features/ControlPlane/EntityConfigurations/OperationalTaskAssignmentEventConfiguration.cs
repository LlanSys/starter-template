using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.WorkTasks.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class OperationalTaskAssignmentEventConfiguration : IEntityTypeConfiguration<OperationalTaskAssignmentEvent>
{
    public void Configure(EntityTypeBuilder<OperationalTaskAssignmentEvent> builder)
    {
        builder.ToTable("OperationalTaskAssignmentEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ActorId).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Action).HasMaxLength(40).IsRequired();
        builder.Property(item => item.PreviousAssigneeUserId).HasMaxLength(128);
        builder.Property(item => item.AssigneeUserId).HasMaxLength(128);
        builder.HasIndex(item => new { item.AssignmentId, item.CreatedAt });
        builder.HasOne<OperationalTaskAssignment>().WithMany().HasForeignKey(item => item.AssignmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
