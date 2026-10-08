using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Data.Seed;

public static class CatalogDemoSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CatalogDemoSeeder");

        try
        {
            await EnsureReferenceDataAsync(db);
            await EnsureDemoCatalogAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Catalog demo seed skipped due to database error.");
        }
    }

    private static async Task EnsureReferenceDataAsync(ScentMatchDbContext db)
    {
        if (!await db.Seasons.AnyAsync())
        {
            db.Seasons.AddRange(
                new Season { Name = "Spring" },
                new Season { Name = "Summer" },
                new Season { Name = "Autumn" },
                new Season { Name = "Winter" },
                new Season { Name = "All Season" });
        }

        if (!await db.FragranceTypes.AnyAsync())
        {
            db.FragranceTypes.AddRange(
                new FragranceType { Name = "Woody" },
                new FragranceType { Name = "Floral" },
                new FragranceType { Name = "Oriental" },
                new FragranceType { Name = "Fresh" },
                new FragranceType { Name = "Citrus" },
                new FragranceType { Name = "Fruity" },
                new FragranceType { Name = "Gourmand" },
                new FragranceType { Name = "Aromatic" },
                new FragranceType { Name = "Chypre" },
                new FragranceType { Name = "Leather" });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoCatalogAsync(ScentMatchDbContext db)
    {
        if (!await db.Brands.AnyAsync(x => x.Name == "Chanel"))
        {
            db.Brands.Add(new Brand
            {
                Name = "Chanel",
                Description = "French luxury fashion and fragrance house.",
                Country = "France",
                Website = "https://www.chanel.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Brands.AnyAsync(x => x.Name == "Dior"))
        {
            db.Brands.Add(new Brand
            {
                Name = "Dior",
                Description = "Christian Dior perfume house.",
                Country = "France",
                Website = "https://www.dior.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var demoNotes = new[]
        {
            ("Bergamot", "Citrus"),
            ("Rose", "Floral"),
            ("Vanilla", "Gourmand"),
            ("Sandalwood", "Woody"),
            ("Jasmine", "Floral")
        };

        foreach (var (name, category) in demoNotes)
        {
            if (!await db.Notes.AnyAsync(x => x.Name == name))
            {
                db.Notes.Add(new Note { Name = name, Category = category });
            }
        }

        await db.SaveChangesAsync();

        if (await db.Perfumes.AnyAsync(x => x.Name == "Bleu de Chanel"))
        {
            return;
        }

        var brand = await db.Brands.FirstAsync(x => x.Name == "Chanel");
        var fragranceType = await db.FragranceTypes.FirstAsync(x => x.Name == "Woody");
        var bergamot = await db.Notes.FirstAsync(x => x.Name == "Bergamot");
        var sandalwood = await db.Notes.FirstAsync(x => x.Name == "Sandalwood");
        var vanilla = await db.Notes.FirstAsync(x => x.Name == "Vanilla");
        var summer = await db.Seasons.FirstAsync(x => x.Name == "Summer");
        var allSeason = await db.Seasons.FirstAsync(x => x.Name == "All Season");

        var perfume = new Perfume
        {
            Name = "Bleu de Chanel",
            BrandId = brand.Id,
            FragranceTypeId = fragranceType.Id,
            Description = "A woody aromatic fragrance for men.",
            ReleaseYear = 2010,
            Concentration = "EDT",
            Gender = "Male",
            Price = 120,
            Rating = 0,
            ReviewsCount = 0,
            Sweetness = 20,
            Freshness = 75,
            Woody = 80,
            Spicy = 35,
            Citrus = 70,
            Floral = 15,
            Fruity = 10,
            Powdery = 10,
            Smoky = 20,
            Oriental = 25,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Perfumes.Add(perfume);
        await db.SaveChangesAsync();

        db.PerfumeNotes.AddRange(
            new PerfumeNote { PerfumeId = perfume.Id, NoteId = bergamot.Id, NoteType = NoteTypes.Top },
            new PerfumeNote { PerfumeId = perfume.Id, NoteId = sandalwood.Id, NoteType = NoteTypes.Middle },
            new PerfumeNote { PerfumeId = perfume.Id, NoteId = vanilla.Id, NoteType = NoteTypes.Base });

        db.PerfumeSeasons.AddRange(
            new PerfumeSeason { PerfumeId = perfume.Id, SeasonId = summer.Id, Suitability = 85 },
            new PerfumeSeason { PerfumeId = perfume.Id, SeasonId = allSeason.Id, Suitability = 70 });

        await db.SaveChangesAsync();
    }
}
