using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScentMatch.Application.Auth.Interfaces;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Comparison.Interfaces;
using ScentMatch.Application.Options;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Application.Profile.Interfaces;
using ScentMatch.Application.Recommendations.Interfaces;
using ScentMatch.Application.Reviews.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Auth;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Comparison;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.History;
using ScentMatch.Infrastructure.Library;
using ScentMatch.Infrastructure.Matching;
using ScentMatch.Infrastructure.Notifications;
using ScentMatch.Infrastructure.Profile;
using ScentMatch.Infrastructure.Recommendations;
using ScentMatch.Infrastructure.Reviews;

namespace ScentMatch.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        services.AddDbContext<ScentMatchDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ScentMatchDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwtSettings = jwtOptions.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.Name
                };
            });

        services.AddAuthorization();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IFragranceTypeService, FragranceTypeService>();
        services.AddScoped<ISeasonService, SeasonService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IPerfumeService, PerfumeService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IFavoritesService, FavoritesService>();
        services.AddScoped<ICollectionsService, CollectionsService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IViewHistoryService, ViewHistoryService>();
        services.AddScoped<ISelectionHistoryService, SelectionHistoryService>();
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IRecommendationHistoryService, RecommendationHistoryService>();
        services.AddScoped<IComparisonService, ComparisonService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}


internal sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.Key) || options.Key.Length < 32)
        {
            return ValidateOptionsResult.Fail("Jwt:Key must be configured and at least 32 characters long.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Jwt:Audience must be configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
