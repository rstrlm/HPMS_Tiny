using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class FolioConfiguration : IEntityTypeConfiguration<Folio>
{
    public void Configure(EntityTypeBuilder<Folio> builder)
    {
        builder.ToTable("Folios");

        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.Customer)
            .WithMany(c => c.Folios)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Reservation)
            .WithMany(r => r.Folios)
            .HasForeignKey(f => f.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => f.CustomerId);
        builder.HasIndex(f => f.ReservationId);
        builder.HasIndex(f => f.Status);
    }
}
