using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> builder)
    {
        builder.ToTable("StaffProfiles");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.KeycloakUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sp => sp.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sp => sp.Email)
            .HasMaxLength(200);

        builder.Property(sp => sp.Skills)
            .HasMaxLength(500);

        builder.HasIndex(sp => sp.KeycloakUserId).IsUnique();
        builder.HasIndex(sp => sp.IsActive);
    }
}
