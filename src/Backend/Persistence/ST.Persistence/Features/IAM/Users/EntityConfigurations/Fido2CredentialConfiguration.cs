using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal class Fido2CredentialConfiguration : IEntityTypeConfiguration<Fido2Credential>
{
    public void Configure(EntityTypeBuilder<Fido2Credential> builder)
    {
        builder.ToTable("Fido2Credentials");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.PublicKey)
            .IsRequired();

        builder.Property(x => x.UserHandle)
            .IsRequired();

        builder.Property(x => x.CredentialId)
            .IsRequired();

        builder.Property(x => x.CredType)
            .HasMaxLength(50);

        builder.HasOne(x => x.User)
            .WithMany(u => u.Fido2Credentials)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Required indexes for querying
        builder.HasIndex(x => x.CredentialId).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
    }
}
