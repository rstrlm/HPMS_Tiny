using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class RoomStateBlockConfiguration : IEntityTypeConfiguration<RoomStateBlock>
{
    public void Configure(EntityTypeBuilder<RoomStateBlock> builder)
    {
        builder.ToTable("RoomStateBlocks");

        builder.HasKey(rsb => rsb.Id);

        builder.Property(rsb => rsb.Note)
            .HasMaxLength(500);

        builder.HasOne(rsb => rsb.Room)
            .WithMany(r => r.StateBlocks)
            .HasForeignKey(rsb => rsb.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rsb => rsb.CreatedByStaff)
            .WithMany()
            .HasForeignKey(rsb => rsb.CreatedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index for overlap queries
        builder.HasIndex(rsb => new { rsb.RoomId, rsb.StartAtUtc, rsb.EndAtUtc, rsb.Type });
    }
}
