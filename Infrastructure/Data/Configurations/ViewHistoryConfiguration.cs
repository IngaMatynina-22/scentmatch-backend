using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class ViewHistoryConfiguration : IEntityTypeConfiguration<ViewHistory>
{
    public void Configure(EntityTypeBuilder<ViewHistory> builder)
    {
        builder.ToTable("view_history");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.PerfumeId);
        builder.HasIndex(x => new { x.UserId, x.PerfumeId })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(x => x.ViewHistory)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.ViewHistory)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
