using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class TreatmentRoomConfiguration : IEntityTypeConfiguration<TreatmentRoom>
{
    public void Configure(EntityTypeBuilder<TreatmentRoom> builder)
    {
        builder.ToTable("TreatmentRooms");

        builder.HasKey(tr => tr.Id);

        builder.Property(tr => tr.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tr => tr.Description)
            .HasMaxLength(500);

        builder.HasIndex(tr => tr.Name);
        builder.HasIndex(tr => tr.IsActive);
    }
}
