using Microsoft.EntityFrameworkCore;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Seed;

public static class ReferenceDataSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = 1,
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "6f1d2c3b-4a5e-6789-abcd-111111111111"
            },
            new Role
            {
                Id = 2,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "6f1d2c3b-4a5e-6789-abcd-222222222222"
            });

        modelBuilder.Entity<Season>().HasData(
            new Season { Id = 1, Name = "Spring" },
            new Season { Id = 2, Name = "Summer" },
            new Season { Id = 3, Name = "Autumn" },
            new Season { Id = 4, Name = "Winter" },
            new Season { Id = 5, Name = "All Season" });

        modelBuilder.Entity<FragranceType>().HasData(
            new FragranceType { Id = 1, Name = "Woody" },
            new FragranceType { Id = 2, Name = "Floral" },
            new FragranceType { Id = 3, Name = "Oriental" },
            new FragranceType { Id = 4, Name = "Fresh" },
            new FragranceType { Id = 5, Name = "Citrus" },
            new FragranceType { Id = 6, Name = "Fruity" },
            new FragranceType { Id = 7, Name = "Gourmand" },
            new FragranceType { Id = 8, Name = "Aromatic" },
            new FragranceType { Id = 9, Name = "Chypre" },
            new FragranceType { Id = 10, Name = "Leather" });
    }
}
