using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class TreatmentAppointmentConfiguration : IEntityTypeConfiguration<TreatmentAppointment>
{
    public void Configure(EntityTypeBuilder<TreatmentAppointment> builder)
    {
        builder.ToTable("TreatmentAppointments");

        builder.HasKey(ta => ta.Id);

        builder.Property(ta => ta.Notes)
            .HasMaxLength(1000);

        builder.HasOne(ta => ta.Customer)
            .WithMany()
            .HasForeignKey(ta => ta.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ta => ta.Reservation)
            .WithMany()
            .HasForeignKey(ta => ta.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ta => ta.TreatmentType)
            .WithMany()
            .HasForeignKey(ta => ta.TreatmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ta => ta.TreatmentRoom)
            .WithMany(tr => tr.Appointments)
            .HasForeignKey(ta => ta.TreatmentRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ta => ta.TherapistStaff)
            .WithMany()
            .HasForeignKey(ta => ta.TherapistStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        // Critical indexes for capacity/overlap queries
        builder.HasIndex(ta => new { ta.TreatmentRoomId, ta.StartAtUtc, ta.EndAtUtc });
        builder.HasIndex(ta => new { ta.TherapistStaffId, ta.StartAtUtc, ta.EndAtUtc });
        builder.HasIndex(ta => ta.CustomerId);
        builder.HasIndex(ta => ta.ReservationId);
        builder.HasIndex(ta => ta.Status);
    }
}
