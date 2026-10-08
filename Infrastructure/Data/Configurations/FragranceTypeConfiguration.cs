using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class FragranceTypeConfiguration : IEntityTypeConfiguration<FragranceType>
{
    public void Configure(EntityTypeBuilder<FragranceType> builder)
    {
        builder.ToTable("fragrance_types");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
