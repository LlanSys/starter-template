using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Shared.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class ModuleDefinitionConfiguration : IEntityTypeConfiguration<ModuleDefinition>
{
    public void Configure(EntityTypeBuilder<ModuleDefinition> builder)
    {
        builder.ToTable("ModuleDefinitions");
        builder.HasKey(module => module.Id);
        builder.Property(module => module.Key).HasMaxLength(80).IsRequired();
        builder.HasIndex(module => module.Key).IsUnique();
        builder.Property(module => module.Name).HasMaxLength(120).IsRequired();
        builder.Property(module => module.Description).HasMaxLength(500);
        builder.Property(module => module.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(module => module.UpdatedBy).HasMaxLength(128);
        builder.Property(module => module.RowVersion).IsRowVersion();
        builder.Ignore(module => module.IsEssential);
        builder.PrimitiveCollection(module => module.DependencyKeys).IsRequired();
        builder.HasData(
            Seed("0194f700-0000-7000-8000-000000000011", "Core", "Core workspace", "Dashboard and workspace navigation."),
            Seed("0194f700-0000-7000-8000-000000000012", "IAM", "Identity and access", "Users, roles, permissions and navigation."),
            Seed("0194f700-0000-7000-8000-000000000013", "Showcase", "Feature showcase", "Implemented example capabilities for the demo workspace."),
            Seed("0194f700-0000-7000-8000-000000000014", "Payments", "Payments", "Payment history, checkout and provider administration."));
    }
    private static ModuleDefinition Seed(string id, string key, string name, string description) => new()
    {
        Id = Guid.Parse(id), Key = key, Name = name, Description = description, DependencyKeys = key == "Core" ? [] : ["core"],
        CreatedBy = ICurrentActorProvider.SystemActor, CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
    };
}
