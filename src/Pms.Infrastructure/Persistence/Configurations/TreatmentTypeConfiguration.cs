using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class TreatmentTypeConfiguration : IEntityTypeConfiguration<TreatmentType>
{
    public void Configure(EntityTypeBuilder<TreatmentType> builder)
    {
        builder.ToTable("TreatmentTypes");

        builder.HasKey(tt => tt.Id);

        builder.Property(tt => tt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tt => tt.Description)
            .HasMaxLength(500);

        builder.Property(tt => tt.BasePrice)
            .HasPrecision(18, 2);

        builder.HasIndex(tt => tt.Name);
        builder.HasIndex(tt => tt.IsActive);
    }
}
