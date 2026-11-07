using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.IAM.Users.Entities;
namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;
internal sealed class AppRoleTenantConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.Property(role => role.ModuleKey).HasMaxLength(80);
        builder.Property(role => role.TemplateKey).HasMaxLength(160);
        builder.Property(role => role.TenantId).IsRequired();
        builder.HasIndex(role => role.NormalizedName).IsUnique(false).HasDatabaseName("IX_Roles_NormalizedName");
        builder.HasIndex(role => new { role.TenantId, role.NormalizedName }).IsUnique().HasDatabaseName("UX_Roles_Tenant_Name");
    }
}
