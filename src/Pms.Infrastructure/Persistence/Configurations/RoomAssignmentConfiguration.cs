using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class RoomAssignmentConfiguration : IEntityTypeConfiguration<RoomAssignment>
{
    public void Configure(EntityTypeBuilder<RoomAssignment> builder)
    {
        builder.ToTable("RoomAssignments");

        builder.HasKey(ra => ra.Id);

        builder.HasOne(ra => ra.Reservation)
            .WithMany(r => r.RoomAssignments)
            .HasForeignKey(ra => ra.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.Room)
            .WithMany(r => r.Assignments)
            .HasForeignKey(ra => ra.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Critical index for overlap queries
        builder.HasIndex(ra => new { ra.RoomId, ra.FromDate, ra.ToDate });
        builder.HasIndex(ra => ra.ReservationId);
    }
}
