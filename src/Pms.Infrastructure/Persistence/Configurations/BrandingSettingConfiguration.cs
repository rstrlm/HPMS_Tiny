using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class BrandingSettingConfiguration : IEntityTypeConfiguration<BrandingSetting>
{
    public void Configure(EntityTypeBuilder<BrandingSetting> builder)
    {
        builder.ToTable("BrandingSettings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.CompanyLegalName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Tagline).HasMaxLength(500);
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.Email).HasMaxLength(200);
        builder.Property(b => b.Phone).HasMaxLength(50);
        builder.Property(b => b.TaxId).HasMaxLength(50);
        builder.Property(b => b.BankName).HasMaxLength(200);
        builder.Property(b => b.IBAN).HasMaxLength(50);
        builder.Property(b => b.BIC).HasMaxLength(20);
    }
}
