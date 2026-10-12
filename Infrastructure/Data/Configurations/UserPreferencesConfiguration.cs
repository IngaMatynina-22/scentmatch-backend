using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("user_preferences");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.MinPrice).HasPrecision(10, 2);
        builder.Property(x => x.MaxPrice).HasPrecision(10, 2);
        builder.Property(x => x.PreferredNoteIds).HasMaxLength(1000);
        builder.Property(x => x.UseBehavioralSignals).HasDefaultValue(true);

        builder.HasOne(x => x.PreferredSeason)
            .WithMany(x => x.UserPreferences)
            .HasForeignKey(x => x.PreferredSeasonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.PreferredFragranceType)
            .WithMany(x => x.UserPreferences)
            .HasForeignKey(x => x.PreferredFragranceTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
