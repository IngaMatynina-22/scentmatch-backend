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
            logger.LogInformation("Сід демо-каталогу успішно виконано.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сід демо-каталогу пропущено через помилку бази даних.");
        }
    }

    private static async Task EnsureReferenceDataAsync(ScentMatchDbContext db)
    {
        if (!await db.Seasons.AnyAsync())
        {
            db.Seasons.AddRange(
                new Season { Name = "Весна" },
                new Season { Name = "Літо" },
                new Season { Name = "Осінь" },
                new Season { Name = "Зима" },
                new Season { Name = "Будь-який сезон" });
        }
        else
        {
            var seasonNames = new Dictionary<string, string>
            {
                ["Spring"] = "Весна", ["Summer"] = "Літо", ["Autumn"] = "Осінь",
                ["Winter"] = "Зима", ["All Season"] = "Будь-який сезон"
            };
            var seasons = await db.Seasons.ToListAsync();
            foreach (var season in seasons)
            {
                if (seasonNames.TryGetValue(season.Name, out var localized))
                {
                    season.Name = localized;
                }
            }
        }

        if (!await db.FragranceTypes.AnyAsync())
        {
            db.FragranceTypes.AddRange(
                new FragranceType { Name = "Деревний" },
                new FragranceType { Name = "Квітковий" },
                new FragranceType { Name = "Східний" },
                new FragranceType { Name = "Свіжий" },
                new FragranceType { Name = "Цитрусовий" },
                new FragranceType { Name = "Фруктовий" },
                new FragranceType { Name = "Гурманський" },
                new FragranceType { Name = "Ароматний" },
                new FragranceType { Name = "Шипровий" },
                new FragranceType { Name = "Шкіряний" });
        }
        else
        {
            var typeNames = new Dictionary<string, string>
            {
                ["Woody"] = "Деревний", ["Floral"] = "Квітковий", ["Oriental"] = "Східний",
                ["Fresh"] = "Свіжий", ["Citrus"] = "Цитрусовий", ["Fruity"] = "Фруктовий",
                ["Gourmand"] = "Гурманський", ["Aromatic"] = "Ароматний",
                ["Chypre"] = "Шипровий", ["Leather"] = "Шкіряний"
            };
            var types = await db.FragranceTypes.ToListAsync();
            foreach (var type in types)
            {
                if (typeNames.TryGetValue(type.Name, out var localized))
                {
                    type.Name = localized;
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoCatalogAsync(ScentMatchDbContext db)
    {
        foreach (var brand in Brands)
        {
            await EnsureBrandAsync(db, brand.Name, brand.Description, brand.Country, brand.Website);
        }

        foreach (var (name, category) in Notes.Concat(ExtraNotes).DistinctBy(x => x.Name))
        {
            var localizedName = LocalizeNote(name);
            var localizedCategory = LocalizeNoteCategory(category);
            if (!await db.Notes.AnyAsync(x => x.Name == localizedName))
            {
                db.Notes.Add(new Note { Name = localizedName, Category = localizedCategory });
            }
        }

        var existingNotes = await db.Notes.ToListAsync();
        foreach (var note in existingNotes)
        {
            note.Name = LocalizeNote(note.Name);
            note.Category = LocalizeNoteCategory(note.Category);
        }

        await db.SaveChangesAsync();

        foreach (var perfume in Perfumes)
        {
            await EnsurePerfumeAsync(db, perfume);
        }
    }

    private static readonly (string Name, string Description, string Country, string Website)[] Brands =
    [
        ("Chanel", "Французький дім моди та парфумерії.", "Франція", "https://www.chanel.com"),
        ("Dior", "Парфумерний дім Christian Dior.", "Франція", "https://www.dior.com"),
        ("Tom Ford", "Розкішний дизайнерський парфумерний бренд.", "США", "https://www.tomford.com"),
        ("Yves Saint Laurent", "Парфумерія YSL Beauty.", "Франція", "https://www.yslbeauty.com"),
        ("Guerlain", "Історичний французький парфумерний дім.", "Франція", "https://www.guerlain.com"),
        ("Hermès", "Парфумерія французького дому Hermès.", "Франція", "https://www.hermes.com"),
        ("Creed", "Нішевий парфумерний дім.", "Франція", "https://www.creedboutique.com"),
        ("Maison Francis Kurkdjian", "Сучасний нішевий парфумерний дім.", "Франція", "https://www.franciskurkdjian.com"),
        ("Byredo", "Шведський нішевий парфумерний бренд.", "Швеція", "https://www.byredo.com"),
        ("Jo Malone", "Британський дім парфумів та одеколонів.", "Великобританія", "https://www.jomalone.com"),
        ("Versace", "Парфумерія італійського модного дому.", "Італія", "https://www.versace.com"),
        ("Armani", "Краса та парфумерія Giorgio Armani.", "Італія", "https://www.armani.com"),
        ("Paco Rabanne", "Смілливі дизайнерські парфуми.", "Іспанія", "https://www.rabanne.com"),
        ("Viktor & Rolf", "Парфуми голландського модного дому.", "Нідерланди", "https://www.viktor-rolf.com"),
        ("Lancôme", "Французький бренд краси та парфумерії.", "Франція", "https://www.lancome.com"),
        ("Mugler", "Ефектні та знакові дизайнерські парфуми.", "Франція", "https://www.mugler.com"),
        ("Prada", "Італійська розкішна парфумерія.", "Італія", "https://www.prada.com"),
        ("Burberry", "Британський розкішний парфумерний дім.", "Великобританія", "https://www.burberry.com"),
        ("Cartier", "Парфумерна лінія французького ювелірного дому.", "Франція", "https://www.cartier.com"),
        ("Acqua di Parma", "Італійський класичний дім одеколонів.", "Італія", "https://www.acquadiparma.com")
    ];

    private static readonly (string Name, string Category)[] Notes =
    [
        ("Бергамот", "Цитрусові"),
        ("Троянда", "Квіткові"),
        ("Ваніль", "Гурманські"),
        ("Сандал", "Деревні"),
        ("Жасмин", "Квіткові"),
        ("Амбра", "Східні"),
        ("Кедр", "Деревні"),
        ("Лимон", "Цитрусові"),
        ("Пачулі", "Деревні"),
        ("Мускус", "Пудрові"),
        ("Перець", "Пряні"),
        ("Ірис", "Пудрові"),
        ("Лаванда", "Ароматні"),
        ("Квітка апельсина", "Квіткові"),
        ("Уд", "Деревні"),
        ("Боби тонка", "Гурманські"),
        ("Яблуко", "Фруктові"),
        ("Ананас", "Фруктові"),
        ("Грейпфрут", "Цитрусові"),
        ("Ветивер", "Деревні"),
        ("Ладан", "Східні"),
        ("Шкіра", "Шкіряні"),
        ("Кардамон", "Пряні"),
        ("Кокос", "Гурманські"),
        ("Півонія", "Квіткові"),
        ("Неролі", "Цитрусові"),
        ("Тютюн", "Деревні"),
        ("Кориця", "Пряні"),
        ("Груша", "Фруктові"),
        ("Морські ноти", "Свіжі")
    ];

    private sealed record SeedPerfume(
        string Name,
        string BrandName,
        string FragranceTypeName,
        string Description,
        int ReleaseYear,
        string Concentration,
        string Gender,
        decimal Price,
        string ImageSeed,
        (int Sweetness, int Freshness, int Woody, int Spicy, int Citrus, int Floral, int Fruity, int Powdery, int Smoky, int Oriental) Characteristics,
        (string NoteName, string NoteType)[] Notes,
        (string SeasonName, int Suitability)[] Seasons);

    private static readonly SeedPerfume[] Perfumes =
    [
        new("Bleu de Chanel", "Chanel", "Woody", "A woody aromatic fragrance for men.", 2010, "EDT", "Male", 120, "bleu-de-chanel",
            (20, 75, 80, 35, 70, 15, 10, 10, 20, 25),
            [("Bergamot", NoteTypes.Top), ("Sandalwood", NoteTypes.Middle), ("Cedar", NoteTypes.Base)],
            [("Summer", 85), ("All Season", 70)]),
        new("Coco Mademoiselle", "Chanel", "Oriental", "Fresh oriental with rose and patchouli.", 2001, "EDP", "Female", 140, "coco-mademoiselle",
            (45, 60, 35, 25, 40, 70, 30, 35, 15, 55),
            [("Orange Blossom", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Patchouli", NoteTypes.Base)],
            [("Spring", 85), ("Autumn", 70)]),
        new("Chance Eau Tendre", "Chanel", "Floral", "Soft floral with grapefruit and jasmine.", 2010, "EDT", "Female", 125, "chance-eau-tendre",
            (35, 70, 15, 10, 55, 85, 40, 30, 5, 20),
            [("Grapefruit", NoteTypes.Top), ("Jasmine", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 80)]),
        new("J'adore", "Dior", "Floral", "A luminous floral fragrance with ylang-ylang and rose.", 1999, "EDP", "Female", 145, "jadore-dior",
            (40, 55, 20, 15, 30, 90, 35, 40, 5, 25),
            [("Jasmine", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 75)]),
        new("Sauvage", "Dior", "Fresh", "A fresh spicy fragrance with bergamot and pepper.", 2015, "EDT", "Male", 130, "sauvage-dior",
            (15, 85, 45, 55, 80, 10, 10, 10, 15, 20),
            [("Bergamot", NoteTypes.Top), ("Pepper", NoteTypes.Middle), ("Amber", NoteTypes.Base)],
            [("Summer", 90), ("Spring", 70)]),
        new("Miss Dior", "Dior", "Floral", "Romantic rose bouquet with peony accents.", 1947, "EDP", "Female", 150, "miss-dior",
            (40, 50, 20, 15, 25, 90, 30, 45, 5, 30),
            [("Pear", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Patchouli", NoteTypes.Base)],
            [("Spring", 95), ("All Season", 70)]),
        new("Homme Intense", "Dior", "Woody", "Intense iris and amber for evening wear.", 2011, "EDP", "Male", 160, "dior-homme-intense",
            (35, 35, 55, 30, 15, 45, 10, 70, 20, 50),
            [("Lavender", NoteTypes.Top), ("Iris", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Autumn", 90), ("Winter", 85)]),
        new("Black Orchid", "Tom Ford", "Oriental", "A rich oriental floral with dark accords.", 2006, "EDP", "Unisex", 210, "black-orchid",
            (55, 25, 40, 45, 10, 60, 30, 35, 40, 85),
            [("Bergamot", NoteTypes.Top), ("Patchouli", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Autumn", 90), ("Winter", 95)]),
        new("Oud Wood", "Tom Ford", "Woody", "Exotic oud with rosewood and sandalwood.", 2007, "EDP", "Unisex", 280, "oud-wood",
            (25, 20, 95, 40, 10, 15, 5, 20, 35, 70),
            [("Cardamom", NoteTypes.Top), ("Oud", NoteTypes.Middle), ("Sandalwood", NoteTypes.Base)],
            [("Autumn", 90), ("Winter", 90)]),
        new("Tobacco Vanille", "Tom Ford", "Gourmand", "Warm tobacco leaf wrapped in vanilla.", 2007, "EDP", "Unisex", 290, "tobacco-vanille",
            (80, 15, 40, 45, 5, 10, 15, 25, 50, 60),
            [("Tobacco", NoteTypes.Top), ("Vanilla", NoteTypes.Middle), ("Tonka Bean", NoteTypes.Base)],
            [("Winter", 95), ("Autumn", 85)]),
        new("Libre", "Yves Saint Laurent", "Floral", "Lavender and orange blossom with a warm base.", 2019, "EDP", "Female", 155, "ysl-libre",
            (45, 50, 25, 20, 35, 80, 25, 30, 10, 35),
            [("Lemon", NoteTypes.Top), ("Lavender", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Spring", 85), ("All Season", 70)]),
        new("Y Eau de Parfum", "Yves Saint Laurent", "Fresh", "Fresh aromatic fougere for everyday wear.", 2017, "EDP", "Male", 135, "ysl-y",
            (20, 80, 40, 35, 55, 15, 15, 15, 10, 20),
            [("Apple", NoteTypes.Top), ("Sage", NoteTypes.Middle), ("Cedar", NoteTypes.Base)],
            [("Summer", 85), ("Spring", 75)]),
        new("Black Opium", "Yves Saint Laurent", "Oriental", "Coffee-vanilla oriental with seductive warmth.", 2014, "EDP", "Female", 150, "black-opium",
            (70, 25, 25, 35, 15, 40, 20, 30, 25, 75),
            [("Pear", NoteTypes.Top), ("Coffee", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Autumn", 90), ("Winter", 90)]),
        new("La Vie Est Belle", "Lancôme", "Gourmand", "Iris gourmand with praline sweetness.", 2012, "EDP", "Female", 140, "la-vie-est-belle",
            (75, 35, 20, 15, 20, 65, 30, 50, 5, 40),
            [("Iris", NoteTypes.Top), ("Jasmine", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Autumn", 80), ("All Season", 70)]),
        new("Idôle", "Lancôme", "Floral", "Clean rose chypre with modern freshness.", 2019, "EDP", "Female", 135, "lancome-idole",
            (35, 60, 25, 15, 30, 85, 25, 40, 5, 25),
            [("Pear", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 75)]),
        new("Shalimar", "Guerlain", "Oriental", "Iconic oriental with vanilla and incense.", 1925, "EDP", "Female", 160, "shalimar",
            (55, 30, 35, 40, 25, 45, 15, 40, 35, 90),
            [("Bergamot", NoteTypes.Top), ("Iris", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Autumn", 85), ("Winter", 90)]),
        new("Habit Rouge", "Guerlain", "Oriental", "Elegant oriental leather for men.", 1965, "EDT", "Male", 145, "habit-rouge",
            (40, 40, 45, 35, 40, 25, 10, 30, 30, 70),
            [("Lemon", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Autumn", 80), ("All Season", 70)]),
        new("Terre d'Hermès", "Hermès", "Woody", "Earthy woody with orange and flint.", 2006, "EDT", "Male", 155, "terre-hermes",
            (15, 55, 85, 40, 60, 10, 15, 15, 25, 30),
            [("Orange Blossom", NoteTypes.Top), ("Pepper", NoteTypes.Middle), ("Vetiver", NoteTypes.Base)],
            [("Autumn", 85), ("Spring", 70)]),
        new("Twilly d'Hermès", "Hermès", "Floral", "Ginger floral with tuberose twist.", 2017, "EDP", "Female", 150, "twilly-hermes",
            (35, 50, 20, 70, 30, 65, 20, 25, 10, 30),
            [("Ginger", NoteTypes.Top), ("Tuberose", NoteTypes.Middle), ("Sandalwood", NoteTypes.Base)],
            [("Spring", 80), ("Summer", 70)]),
        new("Aventus", "Creed", "Fruity", "Pineapple and birch signature niche scent.", 2010, "EDP", "Male", 350, "creed-aventus",
            (25, 70, 55, 30, 40, 15, 75, 15, 35, 25),
            [("Pineapple", NoteTypes.Top), ("Birch", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("All Season", 90), ("Summer", 80)]),
        new("Green Irish Tweed", "Creed", "Fresh", "Green aromatic classic with violet leaf.", 1985, "EDP", "Male", 320, "green-irish-tweed",
            (15, 90, 40, 20, 45, 30, 20, 25, 10, 15),
            [("Lemon", NoteTypes.Top), ("Violet Leaf", NoteTypes.Middle), ("Ambergris", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 85)]),
        new("Baccarat Rouge 540", "Maison Francis Kurkdjian", "Oriental", "Saffron amberwood with mineral radiance.", 2015, "EDP", "Unisex", 320, "br540",
            (50, 40, 45, 35, 10, 30, 10, 25, 20, 80),
            [("Saffron", NoteTypes.Top), ("Amberwood", NoteTypes.Middle), ("Fir Resin", NoteTypes.Base)],
            [("All Season", 90), ("Winter", 80)]),
        new("Gentle Fluidity Gold", "Maison Francis Kurkdjian", "Oriental", "Spiced vanilla nutmeg composition.", 2019, "EDP", "Unisex", 240, "gentle-fluidity-gold",
            (70, 30, 30, 55, 15, 20, 10, 25, 15, 65),
            [("Nutmeg", NoteTypes.Top), ("Vanilla", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Autumn", 90), ("Winter", 85)]),
        new("Gypsy Water", "Byredo", "Woody", "Woody aromatic with juniper and vanilla.", 2008, "EDP", "Unisex", 220, "gypsy-water",
            (35, 55, 60, 25, 30, 25, 15, 25, 20, 30),
            [("Bergamot", NoteTypes.Top), ("Incense", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("All Season", 85), ("Autumn", 75)]),
        new("Blanche", "Byredo", "Floral", "Clean aldehyde floral with rose and sandalwood.", 2009, "EDP", "Unisex", 210, "byredo-blanche",
            (25, 70, 30, 10, 25, 75, 15, 50, 5, 20),
            [("Aldehydes", NoteTypes.Top), ("Rose", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 80)]),
        new("Wood Sage & Sea Salt", "Jo Malone", "Fresh", "Mineral seaside breeze with sage.", 2014, "Cologne", "Unisex", 145, "wood-sage-sea-salt",
            (10, 95, 40, 20, 30, 15, 10, 15, 10, 10),
            [("Sea Notes", NoteTypes.Top), ("Sage", NoteTypes.Middle), ("Ambrette", NoteTypes.Base)],
            [("Summer", 95), ("Spring", 80)]),
        new("English Pear & Freesia", "Jo Malone", "Fruity", "Juicy pear with soft freesia.", 2010, "Cologne", "Female", 140, "english-pear-freesia",
            (40, 60, 15, 10, 25, 70, 80, 30, 5, 15),
            [("Pear", NoteTypes.Top), ("Freesia", NoteTypes.Middle), ("Patchouli", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 85)]),
        new("Eros", "Versace", "Aromatic", "Minty oriental with vanilla sweetness.", 2012, "EDT", "Male", 95, "versace-eros",
            (60, 55, 35, 40, 35, 20, 20, 20, 15, 50),
            [("Mint", NoteTypes.Top), ("Tonka Bean", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Summer", 80), ("All Season", 70)]),
        new("Bright Crystal", "Versace", "Floral", "Fresh peony and yuzu crystal floral.", 2006, "EDT", "Female", 90, "bright-crystal",
            (35, 70, 15, 10, 45, 80, 40, 30, 5, 20),
            [("Yuzu", NoteTypes.Top), ("Peony", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Spring", 90), ("Summer", 85)]),
        new("Acqua di Gio Profondo", "Armani", "Fresh", "Deep aquatic aromatic for men.", 2020, "EDP", "Male", 130, "acqua-di-gio-profondo",
            (15, 95, 35, 25, 50, 10, 15, 10, 10, 20),
            [("Sea Notes", NoteTypes.Top), ("Rosemary", NoteTypes.Middle), ("Mineral Notes", NoteTypes.Base)],
            [("Summer", 95), ("Spring", 80)]),
        new("Si", "Armani", "Chypre", "Modern chypre with blackcurrant and freesia.", 2013, "EDP", "Female", 135, "armani-si",
            (45, 50, 30, 15, 25, 60, 50, 35, 10, 35),
            [("Blackcurrant", NoteTypes.Top), ("Freesia", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("All Season", 85), ("Autumn", 75)]),
        new("1 Million", "Paco Rabanne", "Oriental", "Spicy leather with blood mandarin.", 2008, "EDT", "Male", 90, "one-million",
            (45, 40, 35, 65, 50, 15, 25, 20, 25, 45),
            [("Blood Mandarin", NoteTypes.Top), ("Cinnamon", NoteTypes.Middle), ("Leather", NoteTypes.Base)],
            [("Autumn", 80), ("Winter", 75)]),
        new("Invictus", "Paco Rabanne", "Fresh", "Fresh aquatic with guava and oakmoss.", 2013, "EDT", "Male", 85, "invictus",
            (30, 80, 30, 25, 40, 15, 45, 15, 10, 20),
            [("Grapefruit", NoteTypes.Top), ("Bay Leaf", NoteTypes.Middle), ("Guaiac Wood", NoteTypes.Base)],
            [("Summer", 90), ("Spring", 75)]),
        new("Flowerbomb", "Viktor & Rolf", "Floral", "Explosive floral bouquet with patchouli.", 2005, "EDP", "Female", 140, "flowerbomb",
            (55, 40, 30, 25, 20, 90, 30, 40, 10, 45),
            [("Tea", NoteTypes.Top), ("Jasmine", NoteTypes.Middle), ("Patchouli", NoteTypes.Base)],
            [("Autumn", 85), ("All Season", 75)]),
        new("Spicebomb Extreme", "Viktor & Rolf", "Oriental", "Explosive tobacco spice for cold nights.", 2015, "EDP", "Male", 130, "spicebomb-extreme",
            (45, 25, 40, 90, 15, 10, 10, 20, 40, 50),
            [("Pink Pepper", NoteTypes.Top), ("Tobacco", NoteTypes.Middle), ("Vanilla", NoteTypes.Base)],
            [("Winter", 95), ("Autumn", 90)]),
        new("Angel", "Mugler", "Gourmand", "Iconic patchouli-chocolate gourmand.", 1992, "EDP", "Female", 125, "mugler-angel",
            (85, 20, 40, 30, 10, 35, 25, 30, 20, 55),
            [("Bergamot", NoteTypes.Top), ("Honey", NoteTypes.Middle), ("Patchouli", NoteTypes.Base)],
            [("Autumn", 90), ("Winter", 85)]),
        new("Alien", "Mugler", "Oriental", "Solar jasmine amberwood statement scent.", 2005, "EDP", "Female", 120, "mugler-alien",
            (40, 35, 35, 25, 15, 80, 15, 30, 20, 70),
            [("Jasmine", NoteTypes.Top), ("Cashmeran", NoteTypes.Middle), ("Amber", NoteTypes.Base)],
            [("All Season", 80), ("Winter", 70)]),
        new("Luna Rossa Carbon", "Prada", "Aromatic", "Metallic aromatic fougere with lavender.", 2017, "EDT", "Male", 115, "luna-rossa-carbon",
            (20, 75, 40, 35, 40, 15, 10, 20, 15, 20),
            [("Bergamot", NoteTypes.Top), ("Lavender", NoteTypes.Middle), ("Ambroxan", NoteTypes.Base)],
            [("Summer", 85), ("Spring", 80)]),
        new("Paradoxe", "Prada", "Floral", "Neroli jasmine ambroxan floral.", 2022, "EDP", "Female", 135, "prada-paradoxe",
            (40, 55, 25, 15, 45, 80, 20, 35, 10, 30),
            [("Neroli", NoteTypes.Top), ("Jasmine", NoteTypes.Middle), ("Amber", NoteTypes.Base)],
            [("Spring", 90), ("All Season", 75)]),
        new("Her", "Burberry", "Fruity", "Strawberry floral musk signature.", 2018, "EDP", "Female", 120, "burberry-her",
            (60, 45, 20, 15, 25, 55, 85, 35, 5, 25),
            [("Strawberry", NoteTypes.Top), ("Jasmine", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Spring", 85), ("Summer", 80)]),
        new("Hero", "Burberry", "Woody", "Cedar-focused modern woody for men.", 2021, "EDP", "Male", 125, "burberry-hero",
            (20, 50, 90, 35, 30, 10, 10, 15, 20, 25),
            [("Bergamot", NoteTypes.Top), ("Cedar", NoteTypes.Middle), ("Juniper", NoteTypes.Base)],
            [("Autumn", 85), ("All Season", 75)]),
        new("Declaration", "Cartier", "Aromatic", "Cardamom citrus classic.", 1998, "EDT", "Male", 110, "cartier-declaration",
            (20, 60, 35, 70, 55, 15, 10, 20, 15, 30),
            [("Bergamot", NoteTypes.Top), ("Cardamom", NoteTypes.Middle), ("Cedar", NoteTypes.Base)],
            [("Spring", 80), ("Summer", 75)]),
        new("Colonia", "Acqua di Parma", "Citrus", "Classic Italian citrus cologne.", 1916, "EDC", "Unisex", 140, "adp-colonia",
            (15, 85, 25, 20, 95, 20, 15, 15, 5, 15),
            [("Lemon", NoteTypes.Top), ("Rosemary", NoteTypes.Middle), ("Sandalwood", NoteTypes.Base)],
            [("Summer", 95), ("Spring", 85)]),
        new("Blu Mediterraneo Arancia di Capri", "Acqua di Parma", "Citrus", "Bright Capri orange splash.", 1999, "EDT", "Unisex", 150, "arancia-di-capri",
            (25, 80, 15, 15, 95, 20, 50, 10, 5, 10),
            [("Orange", NoteTypes.Top), ("Cardamom", NoteTypes.Middle), ("Musk", NoteTypes.Base)],
            [("Summer", 95), ("Spring", 90)])
    ];

    // Extra note names used above that must exist
    private static readonly (string Name, string Category)[] ExtraNotes =
    [
        ("Шавлія", "Ароматні"),
        ("Кава", "Гурманські"),
        ("Імбир", "Пряні"),
        ("Тубероза", "Квіткові"),
        ("Береза", "Деревні"),
        ("Амбра сірого", "Східні"),
        ("Шафран", "Пряні"),
        ("Амброве дерево", "Деревні"),
        ("Смола ялиці", "Деревні"),
        ("Мускатний горіх", "Пряні"),
        ("Листя фіалки", "Зелені"),
        ("Альдегіди", "Альдегідні"),
        ("Амбретта", "Мускусні"),
        ("Фрезія", "Квіткові"),
        ("М'ята", "Ароматні"),
        ("Юдзу", "Цитрусові"),
        ("Розмарин", "Ароматні"),
        ("Мінеральні ноти", "Свіжі"),
        ("Чорна смородина", "Фруктові"),
        ("Кров'яний мандарин", "Цитрусові"),
        ("Лавровий лист", "Ароматні"),
        ("Гваякове дерево", "Деревні"),
        ("Чай", "Ароматні"),
        ("Рожевий перець", "Пряні"),
        ("Мед", "Гурманські"),
        ("Кашмеран", "Деревні"),
        ("Амброксан", "Деревні"),
        ("Полуниця", "Фруктові"),
        ("Яловець", "Ароматні"),
        ("Апельсин", "Цитрусові")
    ];

    private static async Task EnsureBrandAsync(
        ScentMatchDbContext db,
        string name,
        string description,
        string country,
        string website)
    {
        var existing = await db.Brands.FirstOrDefaultAsync(x => x.Name == name);
        if (existing is not null)
        {
            existing.Description = description;
            existing.Country = country;
            existing.Website = website;
            existing.IsActive = true;
            await db.SaveChangesAsync();
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

    private static async Task EnsurePerfumeAsync(ScentMatchDbContext db, SeedPerfume seed)
    {
        // Ensure any extra notes referenced by this perfume exist
        foreach (var (noteName, _) in seed.Notes)
        {
            var localizedNoteName = LocalizeNote(noteName);
            if (!await db.Notes.AnyAsync(x => x.Name == localizedNoteName))
            {
                var category = ExtraNotes.FirstOrDefault(x => x.Name == localizedNoteName).Category
                               ?? Notes.FirstOrDefault(x => x.Name == localizedNoteName).Category
                               ?? "Інше";
                if (string.IsNullOrEmpty(category))
                {
                    category = "Інше";
                }

                db.Notes.Add(new Note { Name = localizedNoteName, Category = category });
                await db.SaveChangesAsync();
            }
        }

        var imageUrl = ImageUrl(seed.ImageSeed);
        var shopLinks = BuildShopLinks(seed.Name, seed.BrandName);

        var existing = await db.Perfumes
            .Include(x => x.ShopLinks)
            .FirstOrDefaultAsync(x => x.Name == seed.Name);

        if (existing is not null)
        {
            var changed = false;
            existing.Currency = "UAH";
            existing.Price = seed.Price * 40m;
            existing.Description = BuildUkrainianDescription(seed);
            existing.Gender = LocalizeGender(seed.Gender);
            changed = true;
            if (string.IsNullOrWhiteSpace(existing.ImageUrl))
            {
                existing.ImageUrl = imageUrl;
                changed = true;
            }

            if (existing.ShopLinks.Count == 0)
            {
                foreach (var (store, url, order) in shopLinks)
                {
                    db.PerfumeShopLinks.Add(new PerfumeShopLink
                    {
                        PerfumeId = existing.Id,
                        StoreName = store,
                        Url = url,
                        SortOrder = order
                    });
                }

                changed = true;
            }

            if (changed)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return;
        }

        var brand = await db.Brands.FirstAsync(x => x.Name == seed.BrandName);
        var fragranceType = await db.FragranceTypes.FirstAsync(x => x.Name == LocalizeFragranceType(seed.FragranceTypeName));
        var c = seed.Characteristics;

        var perfume = new Perfume
        {
            Name = seed.Name,
            BrandId = brand.Id,
            FragranceTypeId = fragranceType.Id,
            Description = BuildUkrainianDescription(seed),
            ReleaseYear = seed.ReleaseYear,
            Concentration = seed.Concentration,
            Gender = LocalizeGender(seed.Gender),
            Price = seed.Price * 40m,
            Currency = "UAH",
            ImageUrl = imageUrl,
            Rating = 0,
            ReviewsCount = 0,
            Sweetness = c.Sweetness,
            Freshness = c.Freshness,
            Woody = c.Woody,
            Spicy = c.Spicy,
            Citrus = c.Citrus,
            Floral = c.Floral,
            Fruity = c.Fruity,
            Powdery = c.Powdery,
            Smoky = c.Smoky,
            Oriental = c.Oriental,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Perfumes.Add(perfume);
        await db.SaveChangesAsync();

        foreach (var (noteName, noteType) in seed.Notes)
        {
            var note = await db.Notes.FirstAsync(x => x.Name == LocalizeNote(noteName));
            db.PerfumeNotes.Add(new PerfumeNote
            {
                PerfumeId = perfume.Id,
                NoteId = note.Id,
                NoteType = noteType
            });
        }

        foreach (var (seasonName, suitability) in seed.Seasons)
        {
            // Skip invalid season labels
            var season = await db.Seasons.FirstOrDefaultAsync(x => x.Name == LocalizeSeason(seasonName));
            if (season is null)
            {
                continue;
            }

            db.PerfumeSeasons.Add(new PerfumeSeason
            {
                PerfumeId = perfume.Id,
                SeasonId = season.Id,
                Suitability = suitability
            });
        }

        foreach (var (store, url, order) in shopLinks)
        {
            db.PerfumeShopLinks.Add(new PerfumeShopLink
            {
                PerfumeId = perfume.Id,
                StoreName = store,
                Url = url,
                SortOrder = order
            });
        }

        await db.SaveChangesAsync();
    }

    private static string LocalizeSeason(string name) => name switch
    {
        "Spring" => "Весна",
        "Summer" => "Літо",
        "Autumn" => "Осінь",
        "Winter" => "Зима",
        "All Season" => "Будь-який сезон",
        _ => name
    };

    private static string LocalizeFragranceType(string name) => name switch
    {
        "Woody" => "Деревний",
        "Floral" => "Квітковий",
        "Oriental" => "Східний",
        "Fresh" => "Свіжий",
        "Citrus" => "Цитрусовий",
        "Fruity" => "Фруктовий",
        "Gourmand" => "Гурманський",
        "Aromatic" => "Ароматний",
        "Chypre" => "Шипровий",
        "Leather" => "Шкіряний",
        _ => name
    };

    private static string LocalizeGender(string gender) => gender switch
    {
        "Male" => "Чоловічий",
        "Female" => "Жіночий",
        "Unisex" => "Унісекс",
        _ => gender
    };

    private static string LocalizeNoteCategory(string category) => category switch
    {
        "Citrus" => "Цитрусові",
        "Floral" => "Квіткові",
        "Gourmand" => "Гурманські",
        "Woody" => "Деревні",
        "Oriental" => "Східні",
        "Powdery" => "Пудрові",
        "Spicy" => "Пряні",
        "Aromatic" => "Ароматні",
        "Leather" => "Шкіряні",
        "Fruity" => "Фруктові",
        "Fresh" => "Свіжі",
        "Green" => "Зелені",
        "Aldehydic" => "Альдегідні",
        "Musky" => "Мускусні",
        _ => category
    };

    private static string LocalizeNote(string name) => name switch
    {
        "Bergamot" => "Бергамот",
        "Rose" => "Троянда",
        "Vanilla" => "Ваніль",
        "Sandalwood" => "Сандал",
        "Jasmine" => "Жасмин",
        "Amber" => "Амбра",
        "Cedar" => "Кедр",
        "Lemon" => "Лимон",
        "Patchouli" => "Пачулі",
        "Musk" => "Мускус",
        "Pepper" => "Перець",
        "Iris" => "Ірис",
        "Lavender" => "Лаванда",
        "Orange Blossom" => "Квітка апельсина",
        "Oud" => "Уд",
        "Tonka Bean" => "Боби тонка",
        "Apple" => "Яблуко",
        "Pineapple" => "Ананас",
        "Grapefruit" => "Грейпфрут",
        "Vetiver" => "Ветивер",
        "Incense" => "Ладан",
        "Leather" => "Шкіра",
        "Cardamom" => "Кардамон",
        "Coconut" => "Кокос",
        "Peony" => "Півонія",
        "Neroli" => "Неролі",
        "Tobacco" => "Тютюн",
        "Cinnamon" => "Кориця",
        "Pear" => "Груша",
        "Sea Notes" => "Морські ноти",
        "Sage" => "Шавлія",
        "Coffee" => "Кава",
        "Ginger" => "Імбир",
        "Tuberose" => "Тубероза",
        "Birch" => "Береза",
        "Ambergris" => "Амбра сірого",
        "Saffron" => "Шафран",
        "Amberwood" => "Амброве дерево",
        "Fir Resin" => "Смола ялиці",
        "Nutmeg" => "Мускатний горіх",
        "Violet Leaf" => "Листя фіалки",
        "Aldehydes" => "Альдегіди",
        "Ambrette" => "Амбретта",
        "Freesia" => "Фрезія",
        "Mint" => "М'ята",
        "Yuzu" => "Юдзу",
        "Rosemary" => "Розмарин",
        "Mineral Notes" => "Мінеральні ноти",
        "Blackcurrant" => "Чорна смородина",
        "Blood Mandarin" => "Кров'яний мандарин",
        "Bay Leaf" => "Лавровий лист",
        "Guaiac Wood" => "Гваякове дерево",
        "Tea" => "Чай",
        "Pink Pepper" => "Рожевий перець",
        "Honey" => "Мед",
        "Cashmeran" => "Кашмеран",
        "Ambroxan" => "Амброксан",
        "Strawberry" => "Полуниця",
        "Juniper" => "Яловець",
        "Orange" => "Апельсин",
        _ => name
    };

    private static string BuildUkrainianDescription(SeedPerfume seed) =>
        $"{seed.Name} — {LocalizeFragranceType(seed.FragranceTypeName).ToLowerInvariant()} аромат {LocalizeGender(seed.Gender).ToLowerInvariant()} формату.";

    private static string ImageUrl(string seed) =>
        $"https://picsum.photos/seed/{Uri.EscapeDataString(seed)}/600/800";

    private static (string StoreName, string Url, int SortOrder)[] BuildShopLinks(string perfumeName, string brandName)
    {
        var q = Uri.EscapeDataString($"{brandName} {perfumeName}");
        return
        [
            ("Notino", $"https://www.notino.com/search/?q={q}", 1),
            ("Sephora", $"https://www.sephora.com/search?keyword={q}", 2),
            ("Douglas", $"https://www.douglas.de/de/search?q={q}", 3),
            ("Official brand", BrandOfficialSearch(brandName, perfumeName), 4)
        ];
    }

    private static string BrandOfficialSearch(string brandName, string perfumeName)
    {
        var brand = Brands.FirstOrDefault(b => b.Name == brandName);
        var baseUrl = brand.Website ?? "https://www.google.com/search";
        if (baseUrl.Contains("google.com", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://www.google.com/search?q={Uri.EscapeDataString(brandName + " " + perfumeName + " buy")}";
        }

        // Many brand sites support simple query; fall back to homepage with hash for demo clarity
        return $"{baseUrl.TrimEnd('/')}/?q={Uri.EscapeDataString(perfumeName)}";
    }
}
