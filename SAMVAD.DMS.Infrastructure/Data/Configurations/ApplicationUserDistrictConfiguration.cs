using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMVAD.DMS.Domain.Entities;

namespace SAMVAD.DMS.Infrastructure.Data.Configurations;

public class ApplicationUserDistrictConfiguration : IEntityTypeConfiguration<ApplicationUserDistrict>
{
    public void Configure(EntityTypeBuilder<ApplicationUserDistrict> builder)
    {
        builder.HasKey(x => new { x.ApplicationUserId, x.DistrictId });

        builder.HasOne(x => x.ApplicationUser)
            .WithMany(x => x.AssignedDistricts)
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.District)
            .WithMany(x => x.AssignedAdmins)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}