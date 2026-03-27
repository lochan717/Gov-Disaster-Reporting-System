using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMVAD.DMS.Domain.Entities;

namespace SAMVAD.DMS.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.RoleLabel)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.CreatedOn)
            .IsRequired();
    }
}