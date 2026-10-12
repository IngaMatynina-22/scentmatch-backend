using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class PriceWatchConfiguration : IEntityTypeConfiguration<PriceWatch>
{
    public void Configure(EntityTypeBuilder<PriceWatch> builder)
    {
        builder.ToTable("price_watches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).HasMaxLength(100);
        builder.Property(x => x.SourceUrl).HasMaxLength(1000);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.TargetPrice).HasPrecision(10, 2);
        builder.Property(x => x.LastKnownPrice).HasPrecision(10, 2);
        builder.Property(x => x.LastNotifiedPrice).HasPrecision(10, 2);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PerfumeId });
        builder.HasIndex(x => new { x.IsActive, x.LastCheckedAt });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.PriceWatches)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ShopLink)
            .WithMany()
            .HasForeignKey(x => x.ShopLinkId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.ToTable("price_snapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SourceUrl).HasMaxLength(1000);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.Property(x => x.CapturedAt).IsRequired();

        builder.HasIndex(x => new { x.PerfumeId, x.CapturedAt });
        builder.HasIndex(x => x.PriceWatchId);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.PriceSnapshots)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PriceWatch)
            .WithMany(x => x.Snapshots)
            .HasForeignKey(x => x.PriceWatchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
