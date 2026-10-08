using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class RecommendationHistoryConfiguration : IEntityTypeConfiguration<RecommendationHistory>
{
    public void Configure(EntityTypeBuilder<RecommendationHistory> builder)
    {
        builder.ToTable("recommendation_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(x => x.BatchId)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.PerfumeId);
        builder.HasIndex(x => new { x.UserId, x.BatchId });

        builder.HasOne(x => x.User)
            .WithMany(x => x.RecommendationHistory)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.RecommendationHistory)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
