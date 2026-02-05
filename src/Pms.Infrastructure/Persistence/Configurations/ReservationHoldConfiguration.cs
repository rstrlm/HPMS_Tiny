using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class ReservationHoldConfiguration : IEntityTypeConfiguration<ReservationHold>
{
    public void Configure(EntityTypeBuilder<ReservationHold> builder)
    {
        builder.ToTable("ReservationHolds");

        builder.HasKey(rh => rh.Id);

        builder.Property(rh => rh.SessionId)
            .HasMaxLength(100);

        builder.HasOne(rh => rh.Room)
            .WithMany(r => r.Holds)
            .HasForeignKey(rh => rh.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rh => rh.HeldByStaff)
            .WithMany()
            .HasForeignKey(rh => rh.HeldByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index for overlap and expiry queries
        builder.HasIndex(rh => new { rh.RoomId, rh.FromDate, rh.ToDate });
        builder.HasIndex(rh => rh.ExpiresAtUtc);
    }
}
