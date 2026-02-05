using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> builder)
    {
        builder.ToTable("Charges");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(c => c.VatRate)
            .HasPrecision(5, 4);

        builder.HasOne(c => c.Folio)
            .WithMany(f => f.Charges)
            .HasForeignKey(c => c.FolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CreatedByStaff)
            .WithMany()
            .HasForeignKey(c => c.CreatedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.FolioId);

        // Ignore computed properties - they are not stored in DB
        builder.Ignore(c => c.SubTotal);
        builder.Ignore(c => c.VatAmount);
        builder.Ignore(c => c.Total);
    }
}
