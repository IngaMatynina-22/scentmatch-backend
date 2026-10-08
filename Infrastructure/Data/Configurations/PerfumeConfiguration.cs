using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class PerfumeConfiguration : IEntityTypeConfiguration<Perfume>
{
    public void Configure(EntityTypeBuilder<Perfume> builder)
    {
        builder.ToTable("perfumes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Concentration).HasMaxLength(50);
        builder.Property(x => x.Gender).HasMaxLength(50);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);

        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.Property(x => x.Rating)
            .HasPrecision(3, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.ReviewsCount).HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.BrandId);
        builder.HasIndex(x => x.FragranceTypeId);
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => x.Rating);

        builder.HasOne(x => x.Brand)
            .WithMany(x => x.Perfumes)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FragranceType)
            .WithMany(x => x.Perfumes)
            .HasForeignKey(x => x.FragranceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
