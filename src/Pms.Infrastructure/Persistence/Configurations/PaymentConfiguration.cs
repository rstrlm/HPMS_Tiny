using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.ProviderReference)
            .HasMaxLength(200);

        builder.HasOne(p => p.Folio)
            .WithMany(f => f.Payments)
            .HasForeignKey(p => p.FolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ProcessedByStaff)
            .WithMany()
            .HasForeignKey(p => p.ProcessedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.FolioId);
        builder.HasIndex(p => p.Status);
    }
}
