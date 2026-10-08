using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ScentMatchTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Key", "TEST_ONLY_ScentMatch_Jwt_Signing_Key_32chars!");
        builder.UseSetting("Jwt:Issuer", "ScentMatchTest");
        builder.UseSetting("Jwt:Audience", "ScentMatchTestClients");
        builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "60");
        builder.UseSetting("Jwt:RefreshTokenExpirationDays", "7");
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Server=localhost;Port=3306;Database=scentmatch_test;User=root;Password=root;");

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ScentMatchDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ScentMatchDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ScentMatchDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
