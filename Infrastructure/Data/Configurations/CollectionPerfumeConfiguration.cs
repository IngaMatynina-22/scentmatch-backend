using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class CollectionPerfumeConfiguration : IEntityTypeConfiguration<CollectionPerfume>
{
    public void Configure(EntityTypeBuilder<CollectionPerfume> builder)
    {
        builder.ToTable("collection_perfumes");

        builder.HasKey(x => new { x.CollectionId, x.PerfumeId });

        builder.HasOne(x => x.Collection)
            .WithMany(x => x.CollectionPerfumes)
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.CollectionPerfumes)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
