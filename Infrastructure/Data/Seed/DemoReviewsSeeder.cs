using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Data.Seed;

/// <summary>
/// Seeds approved demo reviews so catalog ratings are non-zero after demo seed.
/// </summary>
public static class DemoReviewsSeeder
{
    private static readonly (string PerfumeName, int Rating, string Text)[] Seeds =
    [
        ("Bleu de Chanel", 5, "Чистий деревний класик — чудовий щоденний аромат."),
        ("Bleu de Chanel", 4, "Свіжий старт і стабільна стійкість."),
        ("Sauvage", 5, "Яскраві бергамот і перець, дуже універсальний."),
        ("Sauvage", 4, "Сильний шлейф; чудово для літа."),
        ("Coco Mademoiselle", 5, "Вишукана рівновага троянди й пачулі."),
        ("Aventus", 5, "Знаковий ананас із березою — вартий своєї ціни."),
        ("Aventus", 3, "Партія може відрізнятися, але це орієнтир."),
        ("Black Orchid", 4, "Насичений і драматичний для вечора."),
        ("Tobacco Vanille", 5, "Теплий гурманський аромат із комфортним нотами."),
        ("Libre", 4, "Сучасне поєднання лаванди й квітів із теплом."),
        ("Terre d'Hermès", 5, "Земляний апельсин і кремінь — неповторний аромат."),
        ("La Vie Est Belle", 4, "Солодкий ірис із праліне."),
        ("Oud Wood", 5, "М'який уд без надмірної важкості."),
        ("Miss Dior", 4, "Романтичний букет троянд."),
        ("Y Eau de Parfum", 3, "Свіжий аромат для щодня, трохи безпечний."),
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoReviewsSeeder");

        try
        {
            var user = await userManager.FindByEmailAsync(DemoUsersSeeder.UserEmail);
            var admin = await userManager.FindByEmailAsync(DemoUsersSeeder.AdminEmail);
            if (user is null || admin is null)
            {
                logger.LogWarning("Demo reviews seed skipped: demo users are missing.");
                return;
            }

            var perfumeNames = Seeds.Select(x => x.PerfumeName).Distinct().ToList();
            var perfumes = await db.Perfumes
                .Where(x => perfumeNames.Contains(x.Name))
                .ToListAsync();

            if (perfumes.Count == 0)
            {
                logger.LogWarning("Demo reviews seed skipped: no matching perfumes.");
                return;
            }

            var perfumeByName = perfumes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var touchedPerfumeIds = new HashSet<int>();
            var added = 0;

            for (var i = 0; i < Seeds.Length; i++)
            {
                var seed = Seeds[i];
                if (!perfumeByName.TryGetValue(seed.PerfumeName, out var perfume))
                {
                    continue;
                }

                // Alternate authors so multi-review averages are realistic.
                var authorId = i % 2 == 0 ? user.Id : admin.Id;

                var exists = await db.Reviews.AnyAsync(x =>
                    x.PerfumeId == perfume.Id && x.UserId == authorId);
                if (exists)
                {
                    continue;
                }

                db.Reviews.Add(new Review
                {
                    UserId = authorId,
                    PerfumeId = perfume.Id,
                    Rating = seed.Rating,
                    Text = seed.Text,
                    Status = ReviewStatuses.Approved,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i + 1)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-(i + 1)),
                });
                touchedPerfumeIds.Add(perfume.Id);
                added++;
            }

            if (added > 0)
            {
                await db.SaveChangesAsync();
            }

            foreach (var perfumeId in touchedPerfumeIds)
            {
                await RecalculatePerfumeRatingAsync(db, perfumeId);
            }

            if (touchedPerfumeIds.Count > 0)
            {
                await db.SaveChangesAsync();
            }

            logger.LogInformation(
                "Demo reviews seed finished. Added {Added} reviews; recalculated {Count} perfumes.",
                added,
                touchedPerfumeIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Demo reviews seed skipped due to an error.");
        }
    }

    private static async Task RecalculatePerfumeRatingAsync(ScentMatchDbContext db, int perfumeId)
    {
        var perfume = await db.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
        if (perfume is null)
        {
            return;
        }

        var approvedRatings = await db.Reviews
            .Where(x => x.PerfumeId == perfumeId && x.Status == ReviewStatuses.Approved)
            .Select(x => x.Rating)
            .ToListAsync();

        if (approvedRatings.Count == 0)
        {
            perfume.Rating = 0m;
            perfume.ReviewsCount = 0;
        }
        else
        {
            perfume.ReviewsCount = approvedRatings.Count;
            perfume.Rating = Math.Round(
                (decimal)approvedRatings.Average(),
                2,
                MidpointRounding.AwayFromZero);
        }

        perfume.UpdatedAt = DateTime.UtcNow;
    }
}
