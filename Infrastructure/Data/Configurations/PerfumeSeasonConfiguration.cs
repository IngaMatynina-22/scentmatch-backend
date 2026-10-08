using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class PerfumeSeasonConfiguration : IEntityTypeConfiguration<PerfumeSeason>
{
    public void Configure(EntityTypeBuilder<PerfumeSeason> builder)
    {
        builder.ToTable("perfume_seasons");

        builder.HasKey(x => new { x.PerfumeId, x.SeasonId });

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.PerfumeSeasons)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Season)
            .WithMany(x => x.PerfumeSeasons)
            .HasForeignKey(x => x.SeasonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
