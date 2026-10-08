using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Rating).IsRequired();

        builder.Property(x => x.Text).HasMaxLength(2000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        builder.HasIndex(x => x.PerfumeId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.PerfumeId })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
