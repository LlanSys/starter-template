using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Support.Entities;
namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;
internal sealed class SupportGrantConfiguration : IEntityTypeConfiguration<SupportGrant>
{
 public void Configure(EntityTypeBuilder<SupportGrant> b) {
 b.ToTable("SupportGrants"); b.HasKey(x=>x.Id);b.HasIndex(x=>new{x.WorkspaceId,x.Status});
 b.Property(x=>x.VendorId).HasMaxLength(128);b.Property(x=>x.Reason).HasMaxLength(1000);b.Property(x=>x.Status).HasMaxLength(24);
 b.Property(x=>x.ApprovedBy).HasMaxLength(128);b.Property(x=>x.ProxyUserId).HasMaxLength(128);b.Property(x=>x.CreatedBy).HasMaxLength(128);b.Property(x=>x.UpdatedBy).HasMaxLength(128);
 b.PrimitiveCollection(x=>x.PermissionKeys).IsRequired();b.Property(x=>x.RowVersion).IsRowVersion();
 }
}
