using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.PerformedByKeycloakId)
            .HasMaxLength(100);

        builder.HasOne(al => al.PerformedByStaff)
            .WithMany()
            .HasForeignKey(al => al.PerformedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(al => new { al.EntityType, al.EntityId });
        builder.HasIndex(al => al.CreatedAtUtc);
    }
}
