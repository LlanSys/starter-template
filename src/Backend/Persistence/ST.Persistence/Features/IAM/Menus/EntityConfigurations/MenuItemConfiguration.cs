using ST.Domain.Features.IAM.Menus.Entities;
using ST.Persistence.Features.IAM.Menus.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.IAM.Menus.EntityConfigurations;

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(menu => menu.Id);

        builder.Property(menu => menu.Key).HasMaxLength(120).IsRequired();
        builder.HasIndex(menu => menu.Key).IsUnique().HasDatabaseName("UX_MenuItems_Key");

        builder.Property(menu => menu.Title).HasMaxLength(120).IsRequired();
        builder.Property(menu => menu.Description).HasMaxLength(300).IsRequired();
        builder.Property(menu => menu.Url).HasMaxLength(240).IsRequired();
        builder.Property(menu => menu.Icon).HasMaxLength(80).IsRequired();
        builder.Property(menu => menu.Placement).HasMaxLength(40).IsRequired();
        builder.Property(menu => menu.RequiredPermissionKey).HasMaxLength(160);
        builder.Property(menu => menu.RequiredModule).HasMaxLength(128);
        builder.Property(menu => menu.DisplayOrder).HasDefaultValue(0);

        builder.Property(menu => menu.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(menu => menu.UpdatedBy).HasMaxLength(120);
        builder.Property(menu => menu.DeletedBy).HasMaxLength(120);

        builder.HasIndex(menu => new { menu.Placement, menu.ParentId, menu.Title })
            .HasDatabaseName("IX_MenuItems_Placement_Parent_Department_Title");

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(menu => menu.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(MenuItemSeed.Items);
    }
}
