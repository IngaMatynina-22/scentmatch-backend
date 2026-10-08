using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Profile.DTOs;
using ScentMatch.Application.Profile.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Profile;

public class ProfileService : IProfileService
{
    private readonly UserManager<User> _userManager;
    private readonly ScentMatchDbContext _dbContext;

    public ProfileService(UserManager<User> userManager, ScentMatchDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(userId);
        return MapProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(userId);
        var username = request.Username.Trim();

        var existingByUsername = await _userManager.FindByNameAsync(username);
        if (existingByUsername is not null && existingByUsername.Id != userId)
        {
            throw new ConflictException("A user with this username already exists.");
        }

        if (!string.Equals(user.UserName, username, StringComparison.Ordinal))
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, username);
            if (!setUserNameResult.Succeeded)
            {
                var errors = string.Join(" ", setUserNameResult.Errors.Select(e => e.Description));
                if (setUserNameResult.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ConflictException("A user with this username already exists.");
                }

                throw new BadRequestException(errors);
            }
        }

        user.FirstName = NormalizeOptional(request.FirstName);
        user.LastName = NormalizeOptional(request.LastName);
        user.AvatarUrl = NormalizeOptional(request.AvatarUrl);
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        return MapProfile(user);
    }

    public async Task<UserPreferencesDto> GetPreferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId);

        var preferences = await GetOrCreatePreferencesAsync(userId, cancellationToken);
        return await MapPreferencesAsync(preferences, cancellationToken);
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(
        int userId,
        UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId);

        if (request.PreferredSeasonId.HasValue)
        {
            var seasonExists = await _dbContext.Seasons
                .AnyAsync(x => x.Id == request.PreferredSeasonId.Value, cancellationToken);
            if (!seasonExists)
            {
                throw new BadRequestException($"Season with id {request.PreferredSeasonId.Value} was not found.");
            }
        }

        if (request.PreferredFragranceTypeId.HasValue)
        {
            var typeExists = await _dbContext.FragranceTypes
                .AnyAsync(x => x.Id == request.PreferredFragranceTypeId.Value, cancellationToken);
            if (!typeExists)
            {
                throw new BadRequestException($"Fragrance type with id {request.PreferredFragranceTypeId.Value} was not found.");
            }
        }

        var preferences = await GetOrCreatePreferencesAsync(userId, cancellationToken);

        preferences.PreferredSeasonId = request.PreferredSeasonId;
        preferences.PreferredFragranceTypeId = request.PreferredFragranceTypeId;
        preferences.Sweetness = request.Sweetness;
        preferences.Freshness = request.Freshness;
        preferences.Woody = request.Woody;
        preferences.Spicy = request.Spicy;
        preferences.Citrus = request.Citrus;
        preferences.Floral = request.Floral;
        preferences.Fruity = request.Fruity;
        preferences.Powdery = request.Powdery;
        preferences.Smoky = request.Smoky;
        preferences.Oriental = request.Oriental;
        preferences.MinPrice = request.MinPrice;
        preferences.MaxPrice = request.MaxPrice;
        preferences.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapPreferencesAsync(preferences, cancellationToken);
    }

    private async Task<User> GetActiveUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new NotFoundException("User profile was not found.");
        }

        return user;
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        _ = await GetActiveUserAsync(userId);
    }

    private async Task<UserPreferences> GetOrCreatePreferencesAsync(int userId, CancellationToken cancellationToken)
    {
        var preferences = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preferences is not null)
        {
            return preferences;
        }

        preferences = new UserPreferences
        {
            UserId = userId,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.UserPreferences.Add(preferences);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return preferences;
    }

    private async Task<UserPreferencesDto> MapPreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken)
    {
        NamedEntityDto? season = null;
        NamedEntityDto? fragranceType = null;

        if (preferences.PreferredSeasonId.HasValue)
        {
            season = await _dbContext.Seasons
                .AsNoTracking()
                .Where(x => x.Id == preferences.PreferredSeasonId.Value)
                .Select(x => new NamedEntityDto { Id = x.Id, Name = x.Name })
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (preferences.PreferredFragranceTypeId.HasValue)
        {
            fragranceType = await _dbContext.FragranceTypes
                .AsNoTracking()
                .Where(x => x.Id == preferences.PreferredFragranceTypeId.Value)
                .Select(x => new NamedEntityDto { Id = x.Id, Name = x.Name })
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new UserPreferencesDto
        {
            PreferredSeason = season,
            PreferredFragranceType = fragranceType,
            Sweetness = preferences.Sweetness,
            Freshness = preferences.Freshness,
            Woody = preferences.Woody,
            Spicy = preferences.Spicy,
            Citrus = preferences.Citrus,
            Floral = preferences.Floral,
            Fruity = preferences.Fruity,
            Powdery = preferences.Powdery,
            Smoky = preferences.Smoky,
            Oriental = preferences.Oriental,
            MinPrice = preferences.MinPrice,
            MaxPrice = preferences.MaxPrice,
            UpdatedAt = preferences.UpdatedAt
        };
    }

    private static UserProfileDto MapProfile(User user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        Username = user.UserName ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        AvatarUrl = user.AvatarUrl,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
