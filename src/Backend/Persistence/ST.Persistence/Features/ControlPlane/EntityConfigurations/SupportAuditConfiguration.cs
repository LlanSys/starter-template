using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Support.Entities;
namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;
internal sealed class SupportAuditConfiguration : IEntityTypeConfiguration<SupportAudit>
{
 public void Configure(EntityTypeBuilder<SupportAudit> b) {
 b.ToTable("SupportAudit");b.HasKey(x=>x.Id);b.HasIndex(x=>new{x.GrantId,x.CreatedAt});
 b.Property(x=>x.ActorId).HasMaxLength(128);b.Property(x=>x.Action).HasMaxLength(400);
 b.HasOne<SupportGrant>().WithMany().HasForeignKey(x=>x.GrantId).OnDelete(DeleteBehavior.Restrict);
 }
}
