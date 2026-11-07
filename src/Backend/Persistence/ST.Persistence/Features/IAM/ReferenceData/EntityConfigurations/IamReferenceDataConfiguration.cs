using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.Persistence.Features.IAM.ReferenceData.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.IAM.ReferenceData.EntityConfigurations;

internal sealed class PermissionContextConfiguration : IEntityTypeConfiguration<PermissionContext>
{
    public void Configure(EntityTypeBuilder<PermissionContext> builder)
    {
        builder.ToTable("PermissionContexts");
        ConfigureBase(builder);
        builder.HasData(IamReferenceDataSeed.PermissionContexts);
    }

    private static void ConfigureBase(EntityTypeBuilder<PermissionContext> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Key }).IsUnique().HasDatabaseName("UX_PermissionContexts_Key");
        builder.Property(item => item.Label).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(120);
    }
}

internal sealed class PermissionResourceConfiguration : IEntityTypeConfiguration<PermissionResource>
{
    public void Configure(EntityTypeBuilder<PermissionResource> builder)
    {
        builder.ToTable("PermissionResources");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Key }).HasDatabaseName("IX_PermissionResources_Key");
        builder.Property(item => item.Label).HasMaxLength(120).IsRequired();
        builder.Property(item => item.ContextKey).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.ContextKey, item.Key }).IsUnique().HasDatabaseName("UX_PermissionResources_Context_Key");
        builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(120);
        builder.HasData(IamReferenceDataSeed.PermissionResources);
    }
}

internal sealed class PermissionActionConfiguration : IEntityTypeConfiguration<PermissionAction>
{
    public void Configure(EntityTypeBuilder<PermissionAction> builder)
    {
        builder.ToTable("PermissionActions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Key }).IsUnique().HasDatabaseName("UX_PermissionActions_Key");
        builder.Property(item => item.Label).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(120);
        builder.HasData(IamReferenceDataSeed.PermissionActions);
    }
}

internal sealed class MenuPlacementConfiguration : IEntityTypeConfiguration<MenuPlacement>
{
    public void Configure(EntityTypeBuilder<MenuPlacement> builder)
    {
        builder.ToTable("MenuPlacements");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Key }).IsUnique().HasDatabaseName("UX_MenuPlacements_Key");
        builder.Property(item => item.Label).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(120);
        builder.HasData(IamReferenceDataSeed.MenuPlacements);
    }
}

internal sealed class MenuIconConfiguration : IEntityTypeConfiguration<MenuIcon>
{
    public void Configure(EntityTypeBuilder<MenuIcon> builder)
    {
        builder.ToTable("MenuIcons");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(80).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Key }).IsUnique().HasDatabaseName("UX_MenuIcons_Key");
        builder.Property(item => item.Label).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(120);
        builder.HasData(IamReferenceDataSeed.MenuIcons);
    }
}

internal sealed class MenuRouteConfiguration : IEntityTypeConfiguration<MenuRoute>
{
    public void Configure(EntityTypeBuilder<MenuRoute> builder)
    {
        builder.ToTable("MenuRoutes");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Key }).IsUnique().HasDatabaseName("UX_MenuRoutes_Key");
        builder.Property(item => item.Label).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Url).HasMaxLength(240).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Url }).IsUnique().HasDatabaseName("UX_MenuRoutes_Url");
        builder.Property(item => item.PlacementKey).HasMaxLength(80).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(120);
        builder.HasData(IamReferenceDataSeed.MenuRoutes);
    }
}
