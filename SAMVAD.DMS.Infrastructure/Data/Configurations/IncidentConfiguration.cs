using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMVAD.DMS.Domain.Entities;

namespace SAMVAD.DMS.Infrastructure.Data.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IncidentId)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.IncidentId)
            .IsUnique();

        builder.Property(x => x.TrackingToken)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.TrackingToken)
            .IsUnique();

        builder.Property(x => x.LocationGpsLat)
            .HasPrecision(10, 7);

        builder.Property(x => x.LocationGpsLng)
            .HasPrecision(10, 7);

        builder.Property(x => x.LocationText)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.ReporterMobile)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.ResolutionNote)
            .HasMaxLength(1000);

        builder.HasOne(x => x.District)
            .WithMany(x => x.Incidents)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}