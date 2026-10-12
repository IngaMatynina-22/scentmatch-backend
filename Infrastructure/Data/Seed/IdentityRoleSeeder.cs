using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Data.Seed;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityRoleSeeder");

        try
        {
            foreach (var roleName in AppRoles.All)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new Role { Name = roleName });
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, errors);
                }
            }
        }
        catch (Exception ex)
        {
            // Match CatalogDemoSeeder / DemoUsersSeeder: do not crash host startup when MySQL is misconfigured.
            logger.LogWarning(ex, "Identity role seed skipped due to database error.");
        }
    }
}
