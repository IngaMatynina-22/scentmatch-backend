using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Application.Recommendations.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Recommendations;

public class RecommendationService : IRecommendationService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ScentMatchDbContext _dbContext;
    private readonly IMatchingService _matchingService;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RecommendationService(
        ScentMatchDbContext dbContext,
        IMatchingService matchingService,
        INotificationDispatcher notificationDispatcher)
    {
        _dbContext = dbContext;
        _matchingService = matchingService;
        _notificationDispatcher = notificationDispatcher;
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
                Message = "Set your preferences or rate perfumes to receive personalized recommendations.",
                Items = Array.Empty<RecommendationDto>()
            };
        }

        var matching = await _matchingService.MatchAsync(matchingRequest, cancellationToken);
        var items = matching.Items.Select(RecommendationDto.FromMatching).ToList();

        if (persistHistory && items.Count > 0)
        {
            await PersistBatchAsync(userId, items, cancellationToken);
            await _notificationDispatcher.NotifyRecommendationsReadyAsync(userId, items.Count, cancellationToken);
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
        var highRated = await _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Status == ReviewStatuses.Approved
                        && x.Rating >= 4
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive)
            .Select(x => new
            {
                x.Perfume.Sweetness,
                x.Perfume.Freshness,
                x.Perfume.Woody,
                x.Perfume.Spicy,
                x.Perfume.Citrus,
                x.Perfume.Floral,
                x.Perfume.Fruity,
                x.Perfume.Powdery,
                x.Perfume.Smoky,
                x.Perfume.Oriental,
                NoteIds = x.Perfume.PerfumeNotes.Select(n => n.NoteId).ToList()
            })
            .ToListAsync(cancellationToken);

        var request = new MatchingRequest
        {
            SeasonId = preferences?.PreferredSeasonId,
            FragranceTypeId = preferences?.PreferredFragranceTypeId,
            Sweetness = preferences?.Sweetness ?? AverageInt(highRated.Select(x => x.Sweetness)),
            Freshness = preferences?.Freshness ?? AverageInt(highRated.Select(x => x.Freshness)),
            Woody = preferences?.Woody ?? AverageInt(highRated.Select(x => x.Woody)),
            Spicy = preferences?.Spicy ?? AverageInt(highRated.Select(x => x.Spicy)),
            Citrus = preferences?.Citrus ?? AverageInt(highRated.Select(x => x.Citrus)),
            Floral = preferences?.Floral ?? AverageInt(highRated.Select(x => x.Floral)),
            Fruity = preferences?.Fruity ?? AverageInt(highRated.Select(x => x.Fruity)),
            Powdery = preferences?.Powdery ?? AverageInt(highRated.Select(x => x.Powdery)),
            Smoky = preferences?.Smoky ?? AverageInt(highRated.Select(x => x.Smoky)),
            Oriental = preferences?.Oriental ?? AverageInt(highRated.Select(x => x.Oriental)),
            MinPrice = preferences?.MinPrice,
            MaxPrice = preferences?.MaxPrice
        };

        var preferredNoteIds = DeserializeNoteIds(preferences?.PreferredNoteIds);
        if (preferredNoteIds.Count > 0)
        {
            request.SelectedNoteIds = preferredNoteIds;
            return request;
        }

        var favoriteNoteIds = await _dbContext.FavoritePerfumes
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive)
            .SelectMany(x => x.Perfume.PerfumeNotes.Select(n => n.NoteId))
            .ToListAsync(cancellationToken);

        var ratedNoteIds = highRated.SelectMany(x => x.NoteIds).ToList();

        var mergedNoteIds = favoriteNoteIds
            .Concat(ratedNoteIds)
            .GroupBy(noteId => noteId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .Take(5)
            .ToList();

        if (mergedNoteIds.Count > 0)
        {
            request.SelectedNoteIds = mergedNoteIds;
        }

        return request;
    }

    private static int? AverageInt(IEnumerable<int?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (present.Count == 0)
        {
            return null;
        }

        return (int)Math.Round(present.Average(), MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyList<int> DeserializeNoteIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<List<int>>(json, JsonOptions) ?? [];
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
