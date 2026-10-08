using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class SelectionHistoryConfiguration : IEntityTypeConfiguration<SelectionHistory>
{
    public void Configure(EntityTypeBuilder<SelectionHistory> builder)
    {
        builder.ToTable("selection_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MinPrice).HasPrecision(10, 2);
        builder.Property(x => x.MaxPrice).HasPrecision(10, 2);

        builder.HasOne(x => x.User)
            .WithMany(x => x.SelectionHistory)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Season)
            .WithMany(x => x.SelectionHistory)
            .HasForeignKey(x => x.SeasonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FragranceType)
            .WithMany(x => x.SelectionHistory)
            .HasForeignKey(x => x.FragranceTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
