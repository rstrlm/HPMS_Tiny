using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class CleaningTaskConfiguration : IEntityTypeConfiguration<CleaningTask>
{
    public void Configure(EntityTypeBuilder<CleaningTask> builder)
    {
        builder.ToTable("CleaningTasks");

        builder.HasKey(ct => ct.Id);

        builder.HasOne(ct => ct.Room)
            .WithMany(r => r.CleaningTasks)
            .HasForeignKey(ct => ct.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.AssignedToStaff)
            .WithMany(s => s.AssignedCleaningTasks)
            .HasForeignKey(ct => ct.AssignedToStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(ct => ct.Notes)
            .HasMaxLength(500);

        // Indexes for querying tasks by date, room, staff, and status
        builder.HasIndex(ct => new { ct.ScheduledDate, ct.Status });
        builder.HasIndex(ct => ct.RoomId);
        builder.HasIndex(ct => ct.AssignedToStaffId);
    }
}
