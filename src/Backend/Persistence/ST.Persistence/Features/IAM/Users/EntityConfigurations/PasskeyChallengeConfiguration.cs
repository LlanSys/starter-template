using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class PasskeyChallengeConfiguration : IEntityTypeConfiguration<PasskeyChallenge>
{
    public void Configure(EntityTypeBuilder<PasskeyChallenge> builder)
    {
        builder.ToTable("PasskeyChallenges");
        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Key).IsRequired().HasMaxLength(128).IsUnicode(false);
        builder.Property(challenge => challenge.OptionsJson).IsRequired();
        builder.Property(challenge => challenge.ExpiresAtUtc).IsRequired();
        builder.HasIndex(challenge => new { challenge.TenantId, challenge.Key }).IsUnique();
        builder.HasIndex(challenge => challenge.ExpiresAtUtc);
    }
}
