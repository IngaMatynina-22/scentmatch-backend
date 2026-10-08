using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class FavoritePerfumeConfiguration : IEntityTypeConfiguration<FavoritePerfume>
{
    public void Configure(EntityTypeBuilder<FavoritePerfume> builder)
    {
        builder.ToTable("favorite_perfumes");

        builder.HasKey(x => new { x.UserId, x.PerfumeId });

        builder.HasOne(x => x.User)
            .WithMany(x => x.FavoritePerfumes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.FavoritePerfumes)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
