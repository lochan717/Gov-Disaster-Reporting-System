using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMVAD.DMS.Domain.Entities;

namespace SAMVAD.DMS.Infrastructure.Data.Configurations;

public class IncidentStatusHistoryConfiguration : IEntityTypeConfiguration<IncidentStatusHistory>
{
    public void Configure(EntityTypeBuilder<IncidentStatusHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChangedById)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.ChangedByName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}