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
            logger.LogInformation("Catalog demo seed completed.");
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
        await EnsureBrandAsync(db, "Chanel", "French luxury fashion and fragrance house.", "France", "https://www.chanel.com");
        await EnsureBrandAsync(db, "Dior", "Christian Dior perfume house.", "France", "https://www.dior.com");
        await EnsureBrandAsync(db, "Tom Ford", "Luxury designer fragrance brand.", "USA", "https://www.tomford.com");
        await EnsureBrandAsync(db, "Yves Saint Laurent", "YSL Beauty fragrances.", "France", "https://www.yslbeauty.com");

        var demoNotes = new[]
        {
            ("Bergamot", "Citrus"),
            ("Rose", "Floral"),
            ("Vanilla", "Gourmand"),
            ("Sandalwood", "Woody"),
            ("Jasmine", "Floral"),
            ("Amber", "Oriental"),
            ("Cedar", "Woody"),
            ("Lemon", "Citrus"),
            ("Patchouli", "Woody"),
            ("Musk", "Powdery"),
            ("Pepper", "Spicy"),
            ("Iris", "Powdery")
        };

        foreach (var (name, category) in demoNotes)
        {
            if (!await db.Notes.AnyAsync(x => x.Name == name))
            {
                db.Notes.Add(new Note { Name = name, Category = category });
            }
        }

        await db.SaveChangesAsync();

        await EnsurePerfumeAsync(
            db,
            name: "Bleu de Chanel",
            brandName: "Chanel",
            fragranceTypeName: "Woody",
            description: "A woody aromatic fragrance for men.",
            releaseYear: 2010,
            concentration: "EDT",
            gender: "Male",
            price: 120,
            characteristics: (20, 75, 80, 35, 70, 15, 10, 10, 20, 25),
            notes: [("Bergamot", NoteTypes.Top), ("Sandalwood", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            seasons: [("Summer", 85), ("All Season", 70)]);

        await EnsurePerfumeAsync(
            db,
            name: "J'adore",
            brandName: "Dior",
            fragranceTypeName: "Floral",
            description: "A luminous floral fragrance with ylang-ylang and rose.",
            releaseYear: 1999,
            concentration: "EDP",
            gender: "Female",
            price: 145,
            characteristics: (40, 55, 20, 15, 30, 90, 35, 40, 5, 25),
            notes: [("Jasmine", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            seasons: [("Spring", 90), ("Summer", 75)]);

        await EnsurePerfumeAsync(
            db,
            name: "Sauvage",
            brandName: "Dior",
            fragranceTypeName: "Fresh",
            description: "A fresh spicy fragrance with bergamot and pepper.",
            releaseYear: 2015,
            concentration: "EDT",
            gender: "Male",
            price: 130,
            characteristics: (15, 85, 45, 55, 80, 10, 10, 10, 15, 20),
            notes: [("Bergamot", NoteTypes.Top), ("Pepper", NoteTypes.Middle), ("Amber", NoteTypes.Base)],
            seasons: [("Summer", 90), ("Spring", 70)]);

        await EnsurePerfumeAsync(
            db,
            name: "Black Orchid",
            brandName: "Tom Ford",
            fragranceTypeName: "Oriental",
            description: "A rich oriental floral with dark accords.",
            releaseYear: 2006,
            concentration: "EDP",
            gender: "Unisex",
            price: 210,
            characteristics: (55, 25, 40, 45, 10, 60, 30, 35, 40, 85),
            notes: [("Bergamot", NoteTypes.Top), ("Patchouli", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            seasons: [("Autumn", 90), ("Winter", 95)]);

        await EnsurePerfumeAsync(
            db,
            name: "Libre",
            brandName: "Yves Saint Laurent",
            fragranceTypeName: "Floral",
            description: "A lavender and orange blossom floral with a warm base.",
            releaseYear: 2019,
            concentration: "EDP",
            gender: "Female",
            price: 155,
            characteristics: (45, 50, 25, 20, 35, 80, 25, 30, 10, 35),
            notes: [("Lemon", NoteTypes.Top), ("Jasmine", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            seasons: [("Spring", 85), ("All Season", 70)]);
    }

    private static async Task EnsureBrandAsync(
        ScentMatchDbContext db,
        string name,
        string description,
        string country,
        string website)
    {
        if (await db.Brands.AnyAsync(x => x.Name == name))
        {
            return;
        }

        db.Brands.Add(new Brand
        {
            Name = name,
            Description = description,
            Country = country,
            Website = website,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsurePerfumeAsync(
        ScentMatchDbContext db,
        string name,
        string brandName,
        string fragranceTypeName,
        string description,
        int releaseYear,
        string concentration,
        string gender,
        decimal price,
        (int Sweetness, int Freshness, int Woody, int Spicy, int Citrus, int Floral, int Fruity, int Powdery, int Smoky, int Oriental) characteristics,
        (string NoteName, string NoteType)[] notes,
        (string SeasonName, int Suitability)[] seasons)
    {
        if (await db.Perfumes.AnyAsync(x => x.Name == name))
        {
            return;
        }

        var brand = await db.Brands.FirstAsync(x => x.Name == brandName);
        var fragranceType = await db.FragranceTypes.FirstAsync(x => x.Name == fragranceTypeName);

        var perfume = new Perfume
        {
            Name = name,
            BrandId = brand.Id,
            FragranceTypeId = fragranceType.Id,
            Description = description,
            ReleaseYear = releaseYear,
            Concentration = concentration,
            Gender = gender,
            Price = price,
            Rating = 0,
            ReviewsCount = 0,
            Sweetness = characteristics.Sweetness,
            Freshness = characteristics.Freshness,
            Woody = characteristics.Woody,
            Spicy = characteristics.Spicy,
            Citrus = characteristics.Citrus,
            Floral = characteristics.Floral,
            Fruity = characteristics.Fruity,
            Powdery = characteristics.Powdery,
            Smoky = characteristics.Smoky,
            Oriental = characteristics.Oriental,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Perfumes.Add(perfume);
        await db.SaveChangesAsync();

        foreach (var (noteName, noteType) in notes)
        {
            var note = await db.Notes.FirstAsync(x => x.Name == noteName);
            db.PerfumeNotes.Add(new PerfumeNote
            {
                PerfumeId = perfume.Id,
                NoteId = note.Id,
                NoteType = noteType
            });
        }

        foreach (var (seasonName, suitability) in seasons)
        {
            var season = await db.Seasons.FirstAsync(x => x.Name == seasonName);
            db.PerfumeSeasons.Add(new PerfumeSeason
            {
                PerfumeId = perfume.Id,
                SeasonId = season.Id,
                Suitability = suitability
            });
        }

        await db.SaveChangesAsync();
    }
}
