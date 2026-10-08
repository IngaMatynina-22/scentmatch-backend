using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Data.Seed;

/// <summary>
/// Applies migrations (optional) and fills the database with roles, reference/catalog demo data, and demo users.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        bool applyMigrations = false,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        if (applyMigrations)
        {
            var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
            logger.LogInformation("Applying EF Core migrations...");
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Migrations applied.");
        }

        logger.LogInformation("Seeding identity roles...");
        await IdentityRoleSeeder.SeedAsync(serviceProvider);

        logger.LogInformation("Seeding catalog demo data...");
        await CatalogDemoSeeder.SeedAsync(serviceProvider);

        logger.LogInformation("Seeding demo users...");
        await DemoUsersSeeder.SeedAsync(serviceProvider);

        logger.LogInformation("Database seed finished successfully.");
    }
}
