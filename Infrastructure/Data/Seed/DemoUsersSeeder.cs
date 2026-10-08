using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Data.Seed;

public static class DemoUsersSeeder
{
    public const string AdminEmail = "admin@scentmatch.local";
    public const string AdminPassword = "Admin123!";
    public const string UserEmail = "user@scentmatch.local";
    public const string UserPassword = "User1234!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoUsersSeeder");

        try
        {
            var admin = await EnsureUserAsync(
                userManager,
                logger,
                email: AdminEmail,
                username: "admin",
                password: AdminPassword,
                firstName: "Demo",
                lastName: "Admin",
                role: AppRoles.Admin);

            var user = await EnsureUserAsync(
                userManager,
                logger,
                email: UserEmail,
                username: "demouser",
                password: UserPassword,
                firstName: "Demo",
                lastName: "User",
                role: AppRoles.User);

            await EnsureUserPreferencesAsync(db, user.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Demo users seed skipped due to an error.");
        }
    }

    private static async Task<User> EnsureUserAsync(
        UserManager<User> userManager,
        ILogger logger,
        string email,
        string username,
        string password,
        string firstName,
        string lastName,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            return existing;
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create demo user {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Created demo user {Email} with role {Role}.", email, role);
        return user;
    }

    private static async Task EnsureUserPreferencesAsync(ScentMatchDbContext db, int userId)
    {
        if (await db.UserPreferences.AnyAsync(x => x.UserId == userId))
        {
            return;
        }

        var seasonId = await db.Seasons
            .Where(x => x.Name == "Summer")
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        var typeId = await db.FragranceTypes
            .Where(x => x.Name == "Woody")
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        var noteIds = await db.Notes
            .Where(x => x.Name == "Bergamot" || x.Name == "Sandalwood")
            .Select(x => x.Id)
            .ToListAsync();

        db.UserPreferences.Add(new UserPreferences
        {
            UserId = userId,
            PreferredSeasonId = seasonId,
            PreferredFragranceTypeId = typeId,
            PreferredNoteIds = noteIds.Count == 0
                ? null
                : System.Text.Json.JsonSerializer.Serialize(noteIds),
            Sweetness = 25,
            Freshness = 70,
            Woody = 75,
            Spicy = 30,
            Citrus = 65,
            Floral = 20,
            Fruity = 15,
            Powdery = 10,
            Smoky = 20,
            Oriental = 25,
            MinPrice = 50,
            MaxPrice = 250,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
