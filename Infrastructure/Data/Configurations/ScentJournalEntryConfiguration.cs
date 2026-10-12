using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class ScentJournalEntryConfiguration : IEntityTypeConfiguration<ScentJournalEntry>
{
    public void Configure(EntityTypeBuilder<ScentJournalEntry> builder)
    {
        builder.ToTable("scent_journal_entries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.OpeningImpression).HasMaxLength(1000);
        builder.Property(x => x.HeartImpression).HasMaxLength(1000);
        builder.Property(x => x.BaseImpression).HasMaxLength(1000);
        builder.Property(x => x.TagsJson).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.TestedOn });
        builder.HasIndex(x => new { x.UserId, x.PerfumeId });
        builder.HasIndex(x => x.Rating);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.ScentJournalEntries)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
