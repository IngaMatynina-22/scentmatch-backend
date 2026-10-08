using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class PerfumeNoteConfiguration : IEntityTypeConfiguration<PerfumeNote>
{
    public void Configure(EntityTypeBuilder<PerfumeNote> builder)
    {
        builder.ToTable("perfume_notes");

        builder.HasKey(x => new { x.PerfumeId, x.NoteId });

        builder.Property(x => x.NoteType)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.PerfumeNotes)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Note)
            .WithMany(x => x.PerfumeNotes)
            .HasForeignKey(x => x.NoteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
