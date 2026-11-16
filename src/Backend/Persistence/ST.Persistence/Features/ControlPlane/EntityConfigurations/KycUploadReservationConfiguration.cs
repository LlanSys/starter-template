using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycUploadReservationConfiguration : IEntityTypeConfiguration<KycUploadReservation>
{
    public void Configure(EntityTypeBuilder<KycUploadReservation> b)
    {
        b.ToTable("KycUploadReservations"); b.HasKey(x => x.Id); b.Property(x => x.State).HasMaxLength(20); b.Property(x => x.Revision).IsConcurrencyToken(); b.HasIndex(x => new { x.State, x.LeaseUntil });
    }
}
