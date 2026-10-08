using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Application.Recommendations.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Recommendations;

public class RecommendationService : IRecommendationService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ScentMatchDbContext _dbContext;
    private readonly IMatchingService _matchingService;

    public RecommendationService(ScentMatchDbContext dbContext, IMatchingService matchingService)
    {
        _dbContext = dbContext;
        _matchingService = matchingService;
    }

    public async Task<RecommendationsResponse> GetRecommendationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await BuildRecommendationsAsync(userId, persistHistory: false, cancellationToken);
    }

    public async Task<RecommendationsResponse> RefreshRecommendationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await BuildRecommendationsAsync(userId, persistHistory: true, cancellationToken);
    }

    private async Task<RecommendationsResponse> BuildRecommendationsAsync(
        int userId,
        bool persistHistory,
        CancellationToken cancellationToken)
    {
        var preferences = await _dbContext.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var matchingRequest = await BuildMatchingRequestAsync(userId, preferences, cancellationToken);
        var preferencesConfigured = HasMatchingCriteria(matchingRequest);

        if (!preferencesConfigured)
        {
            return new RecommendationsResponse
            {
                PreferencesConfigured = false,
                Message = "Set your preferences to receive personalized recommendations.",
                Items = Array.Empty<RecommendationDto>()
            };
        }

        var matching = await _matchingService.MatchAsync(matchingRequest, cancellationToken);
        var items = matching.Items.Select(RecommendationDto.FromMatching).ToList();

        if (persistHistory && items.Count > 0)
        {
            await PersistBatchAsync(userId, items, cancellationToken);
        }

        return new RecommendationsResponse
        {
            PreferencesConfigured = true,
            Message = items.Count == 0
                ? "No matching perfumes found for your current preferences."
                : null,
            Items = items
        };
    }

    private async Task PersistBatchAsync(
        int userId,
        IReadOnlyList<RecommendationDto> items,
        CancellationToken cancellationToken)
    {
        var batchId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        foreach (var item in items)
        {
            _dbContext.RecommendationHistory.Add(new RecommendationHistory
            {
                UserId = userId,
                BatchId = batchId,
                PerfumeId = item.PerfumeId,
                Score = item.Score,
                Reasons = JsonSerializer.Serialize(item.Reasons, JsonOptions),
                CreatedAt = createdAt
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MatchingRequest> BuildMatchingRequestAsync(
        int userId,
        UserPreferences? preferences,
        CancellationToken cancellationToken)
    {
        var request = new MatchingRequest
        {
            SeasonId = preferences?.PreferredSeasonId,
            FragranceTypeId = preferences?.PreferredFragranceTypeId,
            Sweetness = preferences?.Sweetness,
            Freshness = preferences?.Freshness,
            Woody = preferences?.Woody,
            Spicy = preferences?.Spicy,
            Citrus = preferences?.Citrus,
            Floral = preferences?.Floral,
            Fruity = preferences?.Fruity,
            Powdery = preferences?.Powdery,
            Smoky = preferences?.Smoky,
            Oriental = preferences?.Oriental,
            MinPrice = preferences?.MinPrice,
            MaxPrice = preferences?.MaxPrice
        };

        // Enrich with frequent notes from favorites when preferences do not specify notes.
        var favoriteNoteIds = await _dbContext.FavoritePerfumes
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive)
            .SelectMany(x => x.Perfume.PerfumeNotes.Select(n => n.NoteId))
            .GroupBy(noteId => noteId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (favoriteNoteIds.Count > 0)
        {
            request.SelectedNoteIds = favoriteNoteIds;
        }

        return request;
    }

    private static bool HasMatchingCriteria(MatchingRequest request)
    {
        return request.SeasonId.HasValue
               || request.FragranceTypeId.HasValue
               || request.MinPrice.HasValue
               || request.MaxPrice.HasValue
               || (request.SelectedNoteIds is { Count: > 0 })
               || request.Sweetness.HasValue
               || request.Freshness.HasValue
               || request.Woody.HasValue
               || request.Spicy.HasValue
               || request.Citrus.HasValue
               || request.Floral.HasValue
               || request.Fruity.HasValue
               || request.Powdery.HasValue
               || request.Smoky.HasValue
               || request.Oriental.HasValue;
    }
}
