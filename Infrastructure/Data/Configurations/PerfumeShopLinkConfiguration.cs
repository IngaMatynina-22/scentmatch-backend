using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Configurations;

public class PerfumeShopLinkConfiguration : IEntityTypeConfiguration<PerfumeShopLink>
{
    public void Configure(EntityTypeBuilder<PerfumeShopLink> builder)
    {
        builder.ToTable("perfume_shop_links");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoreName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(x => x.PerfumeId);

        builder.HasOne(x => x.Perfume)
            .WithMany(x => x.ShopLinks)
            .HasForeignKey(x => x.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
