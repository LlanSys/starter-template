using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class TenantAdministratorInvitationConfiguration : IEntityTypeConfiguration<TenantAdministratorInvitation>
{
    public void Configure(EntityTypeBuilder<TenantAdministratorInvitation> builder)
    {
        builder.ToTable("TenantAdministratorInvitations");
        builder.HasKey(invitation => invitation.Id);
        builder.HasIndex(invitation => invitation.TenantId).IsUnique();
        builder.HasIndex(invitation => new { invitation.State, invitation.DeliveredAt, invitation.NextAttemptAt });
        builder.HasOne(invitation => invitation.Tenant).WithMany().HasForeignKey(invitation => invitation.TenantId);
        builder.Property(invitation => invitation.Revision).IsConcurrencyToken();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(64);
        builder.Property(invitation => invitation.Email).HasMaxLength(256);
        builder.Property(invitation => invitation.FirstName).HasMaxLength(100);
        builder.Property(invitation => invitation.LastName).HasMaxLength(100);
        builder.Property(invitation => invitation.CreatedBy).HasMaxLength(128);
        builder.Property(invitation => invitation.ChangedBy).HasMaxLength(128);
        builder.Property(invitation => invitation.ClaimedUserId).HasMaxLength(128);
    }
}
