using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Application.Recommendations.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.Matching;

namespace ScentMatch.Infrastructure.Recommendations;

public class RecommendationService : IRecommendationService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ScentMatchDbContext _dbContext;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RecommendationService(
        ScentMatchDbContext dbContext,
        INotificationDispatcher notificationDispatcher)
    {
        _dbContext = dbContext;
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

        var profiles = await PerfumeProfileLoader.LoadActiveAsync(_dbContext, cancellationToken);
        var items = profiles
            .Select(p =>
            {
                var breakdown = MatchingCriteriaScorer.Score(p, matchingRequest);
                if (breakdown.TotalScore <= 0m)
                {
                    return null;
                }

                var result = MatchingCriteriaScorer.ToMatchingResult(p, breakdown);
                return RecommendationDto.FromMatching(result);
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PerfumeId)
            .Take(MatchingWeights.MaxResults)
            .ToList();

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
        var useBehavioral = preferences?.UseBehavioralSignals ?? true;
        var resetAt = preferences?.PersonalizationResetAt;

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

        var preferredNoteIds = DeserializeNoteIds(preferences?.PreferredNoteIds);
        if (preferredNoteIds.Count > 0)
        {
            request.SelectedNoteIds = preferredNoteIds;
        }

        if (!useBehavioral)
        {
            return request;
        }

        var highRated = await LoadHighRatedSignalsAsync(userId, resetAt, cancellationToken);

        request.Sweetness ??= AverageInt(highRated.Select(x => x.Sweetness));
        request.Freshness ??= AverageInt(highRated.Select(x => x.Freshness));
        request.Woody ??= AverageInt(highRated.Select(x => x.Woody));
        request.Spicy ??= AverageInt(highRated.Select(x => x.Spicy));
        request.Citrus ??= AverageInt(highRated.Select(x => x.Citrus));
        request.Floral ??= AverageInt(highRated.Select(x => x.Floral));
        request.Fruity ??= AverageInt(highRated.Select(x => x.Fruity));
        request.Powdery ??= AverageInt(highRated.Select(x => x.Powdery));
        request.Smoky ??= AverageInt(highRated.Select(x => x.Smoky));
        request.Oriental ??= AverageInt(highRated.Select(x => x.Oriental));

        if (request.SelectedNoteIds is { Count: > 0 })
        {
            return request;
        }

        var favoriteQuery = _dbContext.FavoritePerfumes
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive);

        if (resetAt.HasValue)
        {
            favoriteQuery = favoriteQuery.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var favoriteNoteIds = await favoriteQuery
            .SelectMany(x => x.Perfume.PerfumeNotes.Select(n => n.NoteId))
            .ToListAsync(cancellationToken);

        // Personal selection history only (exclude Gift / Text).
        var historyQuery = _dbContext.SelectionHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ScenarioType == MatchingScenarioTypes.Personal);

        if (resetAt.HasValue)
        {
            historyQuery = historyQuery.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var historyNoteJson = await historyQuery
            .Select(x => x.SelectedNoteIds)
            .ToListAsync(cancellationToken);

        var historyNoteIds = historyNoteJson
            .SelectMany(DeserializeNoteIds)
            .ToList();

        var ratedNoteIds = highRated.SelectMany(x => x.NoteIds).ToList();

        var mergedNoteIds = favoriteNoteIds
            .Concat(ratedNoteIds)
            .Concat(historyNoteIds)
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

    private async Task<List<RatedPerfumeSignal>> LoadHighRatedSignalsAsync(
        int userId,
        DateTime? resetAt,
        CancellationToken cancellationToken)
    {
        var reviewQuery = _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Status == ReviewStatuses.Approved
                        && x.Rating >= 4
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive);

        if (resetAt.HasValue)
        {
            reviewQuery = reviewQuery.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var fromReviews = await reviewQuery
            .Select(x => new RatedPerfumeSignal
            {
                Sweetness = x.Perfume.Sweetness,
                Freshness = x.Perfume.Freshness,
                Woody = x.Perfume.Woody,
                Spicy = x.Perfume.Spicy,
                Citrus = x.Perfume.Citrus,
                Floral = x.Perfume.Floral,
                Fruity = x.Perfume.Fruity,
                Powdery = x.Perfume.Powdery,
                Smoky = x.Perfume.Smoky,
                Oriental = x.Perfume.Oriental,
                NoteIds = x.Perfume.PerfumeNotes.Select(n => n.NoteId).ToList()
            })
            .ToListAsync(cancellationToken);

        var journalQuery = _dbContext.ScentJournalEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Rating >= 4
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive);

        if (resetAt.HasValue)
        {
            journalQuery = journalQuery.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var fromJournal = await journalQuery
            .Select(x => new RatedPerfumeSignal
            {
                Sweetness = x.Perfume.Sweetness,
                Freshness = x.Perfume.Freshness,
                Woody = x.Perfume.Woody,
                Spicy = x.Perfume.Spicy,
                Citrus = x.Perfume.Citrus,
                Floral = x.Perfume.Floral,
                Fruity = x.Perfume.Fruity,
                Powdery = x.Perfume.Powdery,
                Smoky = x.Perfume.Smoky,
                Oriental = x.Perfume.Oriental,
                NoteIds = x.Perfume.PerfumeNotes.Select(n => n.NoteId).ToList()
            })
            .ToListAsync(cancellationToken);

        return fromReviews.Concat(fromJournal).ToList();
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
               || request.Intensity.HasValue
               || !string.IsNullOrWhiteSpace(request.Concentration)
               || !string.IsNullOrWhiteSpace(request.Gender)
               || (request.SelectedNoteIds is { Count: > 0 })
               || (request.ExcludedNoteIds is { Count: > 0 })
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

    private sealed class RatedPerfumeSignal
    {
        public int? Sweetness { get; init; }
        public int? Freshness { get; init; }
        public int? Woody { get; init; }
        public int? Spicy { get; init; }
        public int? Citrus { get; init; }
        public int? Floral { get; init; }
        public int? Fruity { get; init; }
        public int? Powdery { get; init; }
        public int? Smoky { get; init; }
        public int? Oriental { get; init; }
        public List<int> NoteIds { get; init; } = [];
    }
}
