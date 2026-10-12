using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class BlindComparisonSessionConfiguration : IEntityTypeConfiguration<BlindComparisonSession>
{
    public void Configure(EntityTypeBuilder<BlindComparisonSession> builder)
    {
        builder.ToTable("blind_comparison_sessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.Property(x => x.WinnerLabel).HasMaxLength(10);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.Status, x.ExpiresAt });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BlindComparisonSlotConfiguration : IEntityTypeConfiguration<BlindComparisonSlot>
{
    public void Configure(EntityTypeBuilder<BlindComparisonSlot> builder)
    {
        builder.ToTable("blind_comparison_slots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Label).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Comment).HasMaxLength(1000);

        builder.HasIndex(x => new { x.SessionId, x.Label }).IsUnique();
        builder.HasIndex(x => new { x.SessionId, x.PerfumeId }).IsUnique();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany()
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
