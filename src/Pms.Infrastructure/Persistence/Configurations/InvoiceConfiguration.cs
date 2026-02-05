using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.VatTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.GrandTotal)
            .HasPrecision(18, 2);

        builder.HasOne(i => i.Folio)
            .WithMany(f => f.Invoices)
            .HasForeignKey(i => i.FolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.IssuedByStaff)
            .WithMany()
            .HasForeignKey(i => i.IssuedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.FolioId);
        builder.HasIndex(i => i.Status);
    }
}
