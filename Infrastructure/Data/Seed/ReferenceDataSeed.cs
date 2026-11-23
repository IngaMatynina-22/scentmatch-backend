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
            new Season { Id = 1, Name = "Весна" },
            new Season { Id = 2, Name = "Літо" },
            new Season { Id = 3, Name = "Осінь" },
            new Season { Id = 4, Name = "Зима" },
            new Season { Id = 5, Name = "Будь-який сезон" });

        modelBuilder.Entity<FragranceType>().HasData(
            new FragranceType { Id = 1, Name = "Деревний" },
            new FragranceType { Id = 2, Name = "Квітковий" },
            new FragranceType { Id = 3, Name = "Східний" },
            new FragranceType { Id = 4, Name = "Свіжий" },
            new FragranceType { Id = 5, Name = "Цитрусовий" },
            new FragranceType { Id = 6, Name = "Фруктовий" },
            new FragranceType { Id = 7, Name = "Гурманський" },
            new FragranceType { Id = 8, Name = "Ароматний" },
            new FragranceType { Id = 9, Name = "Шипровий" },
            new FragranceType { Id = 10, Name = "Шкіряний" });
    }
}
