using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.IAM.Users.Entities;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class SsoAuthorizationCodeConfiguration : IEntityTypeConfiguration<SsoAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<SsoAuthorizationCode> builder)
    {
        builder.ToTable("SsoAuthorizationCodes");
        builder.HasKey(code => code.Hash);
        builder.Property(code => code.Hash).HasMaxLength(64);
        builder.Property(code => code.SecurityStampHash).HasMaxLength(64);
        builder.Property(code => code.UserId).HasMaxLength(128);
        builder.Property(code => code.ConnectionProvider).HasMaxLength(32);
        builder.HasIndex(code => code.ExpiresAt);
    }
}
