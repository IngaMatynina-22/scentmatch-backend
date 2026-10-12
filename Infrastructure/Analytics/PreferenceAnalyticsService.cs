using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Analytics.DTOs;
using ScentMatch.Application.Analytics.Interfaces;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Analytics;

public class PreferenceAnalyticsService : IPreferenceAnalyticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new();
    private const int MinRatingsForSection = 3;
    private const int MinFavoritesForSection = 2;
    private const int MinSelectionsForSection = 2;
    private const int TopN = 5;

    private readonly ScentMatchDbContext _dbContext;

    public PreferenceAnalyticsService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PreferenceAnalyticsDto> GetPreferencesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var preferences = await _dbContext.UserPreferences
            .AsNoTracking()
            .Include(x => x.PreferredSeason)
            .Include(x => x.PreferredFragranceType)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var resetAt = preferences?.PersonalizationResetAt;

        return new PreferenceAnalyticsDto
        {
            ExplicitPreferences = await BuildExplicitAsync(preferences, cancellationToken),
            ReviewRatings = await BuildReviewRatingsAsync(userId, resetAt, cancellationToken),
            JournalRatings = await BuildJournalRatingsAsync(userId, resetAt, cancellationToken),
            Favorites = await BuildFavoritesAsync(userId, resetAt, cancellationToken),
            Collections = await BuildCollectionsAsync(userId, cancellationToken),
            PersonalSelectionHistory = await BuildSelectionHistoryAsync(userId, resetAt, cancellationToken),
            Personalization = new PersonalizationStateDto
            {
                UseBehavioralSignals = preferences?.UseBehavioralSignals ?? true,
                PersonalizationResetAt = preferences?.PersonalizationResetAt
            }
        };
    }

    public async Task<ResetPersonalizationResponse> ResetPersonalizationAsync(
        int userId,
        ResetPersonalizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var preferences = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preferences is null)
        {
            preferences = new UserPreferences
            {
                UserId = userId,
                UseBehavioralSignals = true
            };
            _dbContext.UserPreferences.Add(preferences);
        }

        preferences.PersonalizationResetAt = DateTime.UtcNow;
        if (request.UseBehavioralSignals.HasValue)
        {
            preferences.UseBehavioralSignals = request.UseBehavioralSignals.Value;
        }

        preferences.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ResetPersonalizationResponse
        {
            PersonalizationResetAt = preferences.PersonalizationResetAt.Value,
            UseBehavioralSignals = preferences.UseBehavioralSignals
        };
    }

    private async Task<ExplicitPreferencesSection?> BuildExplicitAsync(
        UserPreferences? preferences,
        CancellationToken cancellationToken)
    {
        if (preferences is null)
        {
            return null;
        }

        var hasAny =
            preferences.PreferredSeasonId.HasValue
            || preferences.PreferredFragranceTypeId.HasValue
            || preferences.MinPrice.HasValue
            || preferences.MaxPrice.HasValue
            || !string.IsNullOrWhiteSpace(preferences.PreferredNoteIds)
            || preferences.Sweetness.HasValue
            || preferences.Freshness.HasValue
            || preferences.Woody.HasValue
            || preferences.Spicy.HasValue
            || preferences.Citrus.HasValue
            || preferences.Floral.HasValue
            || preferences.Fruity.HasValue
            || preferences.Powdery.HasValue
            || preferences.Smoky.HasValue
            || preferences.Oriental.HasValue;

        if (!hasAny)
        {
            return null;
        }

        var noteIds = DeserializeNoteIds(preferences.PreferredNoteIds);
        List<NamedEntityDto>? notes = null;
        if (noteIds.Count > 0)
        {
            notes = await _dbContext.Notes
                .AsNoTracking()
                .Where(x => noteIds.Contains(x.Id))
                .Select(x => new NamedEntityDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken);
        }

        return new ExplicitPreferencesSection
        {
            PreferredSeason = preferences.PreferredSeason is null
                ? null
                : new NamedEntityDto { Id = preferences.PreferredSeason.Id, Name = preferences.PreferredSeason.Name },
            PreferredFragranceType = preferences.PreferredFragranceType is null
                ? null
                : new NamedEntityDto
                {
                    Id = preferences.PreferredFragranceType.Id,
                    Name = preferences.PreferredFragranceType.Name
                },
            PreferredNotes = notes,
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
            MaxPrice = preferences.MaxPrice
        };
    }

    private async Task<RatingSignalsSection?> BuildReviewRatingsAsync(
        int userId,
        DateTime? resetAt,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == ReviewStatuses.Approved);

        if (resetAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var rows = await query
            .Select(x => new
            {
                x.Rating,
                FragranceTypeId = x.Perfume.FragranceTypeId,
                FragranceTypeName = x.Perfume.FragranceType.Name,
                NoteIds = x.Perfume.PerfumeNotes.Select(n => new { n.NoteId, n.Note.Name }).ToList()
            })
            .ToListAsync(cancellationToken);

        if (rows.Count < MinRatingsForSection)
        {
            return null;
        }

        return new RatingSignalsSection
        {
            RatedCount = rows.Count,
            AverageRating = Math.Round((decimal)rows.Average(x => x.Rating), 2, MidpointRounding.AwayFromZero),
            TopNotes = TopNamed(
                rows.SelectMany(r => r.NoteIds.Select(n => (n.NoteId, n.Name)))),
            TopFragranceTypes = TopNamed(
                rows.Select(r => (r.FragranceTypeId, r.FragranceTypeName)))
        };
    }

    private async Task<RatingSignalsSection?> BuildJournalRatingsAsync(
        int userId,
        DateTime? resetAt,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ScentJournalEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (resetAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var rows = await query
            .Select(x => new
            {
                x.Rating,
                FragranceTypeId = x.Perfume.FragranceTypeId,
                FragranceTypeName = x.Perfume.FragranceType.Name,
                NoteIds = x.Perfume.PerfumeNotes.Select(n => new { n.NoteId, n.Note.Name }).ToList()
            })
            .ToListAsync(cancellationToken);

        if (rows.Count < MinRatingsForSection)
        {
            return null;
        }

        return new RatingSignalsSection
        {
            RatedCount = rows.Count,
            AverageRating = Math.Round((decimal)rows.Average(x => x.Rating), 2, MidpointRounding.AwayFromZero),
            TopNotes = TopNamed(
                rows.SelectMany(r => r.NoteIds.Select(n => (n.NoteId, n.Name)))),
            TopFragranceTypes = TopNamed(
                rows.Select(r => (r.FragranceTypeId, r.FragranceTypeName)))
        };
    }

    private async Task<FavoritesSection?> BuildFavoritesAsync(
        int userId,
        DateTime? resetAt,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.FavoritePerfumes
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (resetAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var rows = await query
            .Select(x => new
            {
                FragranceTypeId = x.Perfume.FragranceTypeId,
                FragranceTypeName = x.Perfume.FragranceType.Name,
                NoteIds = x.Perfume.PerfumeNotes.Select(n => new { n.NoteId, n.Note.Name }).ToList()
            })
            .ToListAsync(cancellationToken);

        if (rows.Count < MinFavoritesForSection)
        {
            return null;
        }

        return new FavoritesSection
        {
            Count = rows.Count,
            TopNotes = TopNamed(rows.SelectMany(r => r.NoteIds.Select(n => (n.NoteId, n.Name)))),
            TopFragranceTypes = TopNamed(rows.Select(r => (r.FragranceTypeId, r.FragranceTypeName)))
        };
    }

    private async Task<CollectionsSection?> BuildCollectionsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var collections = await _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.ScenarioKey,
                PerfumeCount = x.CollectionPerfumes.Count
            })
            .ToListAsync(cancellationToken);

        if (collections.Count == 0)
        {
            return null;
        }

        var byScenario = collections
            .Where(c => !string.IsNullOrWhiteSpace(c.ScenarioKey))
            .GroupBy(c => c.ScenarioKey!)
            .Select(g => new NamedCountDto
            {
                Name = g.Key,
                Count = g.Sum(x => x.PerfumeCount)
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CollectionsSection
        {
            TotalCollections = collections.Count,
            TotalPerfumes = collections.Sum(x => x.PerfumeCount),
            CountsByScenario = byScenario.Count == 0 ? null : byScenario
        };
    }

    private async Task<SelectionHistorySection?> BuildSelectionHistoryAsync(
        int userId,
        DateTime? resetAt,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.SelectionHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.ScenarioType == MatchingScenarioTypes.Personal);

        if (resetAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= resetAt.Value);
        }

        var rows = await query
            .Select(x => new
            {
                x.SeasonId,
                SeasonName = x.Season != null ? x.Season.Name : null,
                x.FragranceTypeId,
                FragranceTypeName = x.FragranceType != null ? x.FragranceType.Name : null,
                x.SelectedNoteIds
            })
            .ToListAsync(cancellationToken);

        if (rows.Count < MinSelectionsForSection)
        {
            return null;
        }

        var noteIdCounts = new Dictionary<int, int>();
        foreach (var row in rows)
        {
            foreach (var noteId in DeserializeNoteIds(row.SelectedNoteIds))
            {
                noteIdCounts[noteId] = noteIdCounts.GetValueOrDefault(noteId) + 1;
            }
        }

        IReadOnlyList<NamedCountDto>? topNotes = null;
        if (noteIdCounts.Count > 0)
        {
            var topIds = noteIdCounts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(TopN)
                .Select(kv => kv.Key)
                .ToList();

            var names = await _dbContext.Notes
                .AsNoTracking()
                .Where(n => topIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name, cancellationToken);

            topNotes = topIds
                .Where(id => names.ContainsKey(id))
                .Select(id => new NamedCountDto
                {
                    Id = id,
                    Name = names[id],
                    Count = noteIdCounts[id]
                })
                .ToList();
        }

        return new SelectionHistorySection
        {
            Count = rows.Count,
            TopSeasons = TopNamed(
                rows.Where(r => r.SeasonId.HasValue && r.SeasonName is not null)
                    .Select(r => (r.SeasonId!.Value, r.SeasonName!))),
            TopFragranceTypes = TopNamed(
                rows.Where(r => r.FragranceTypeId.HasValue && r.FragranceTypeName is not null)
                    .Select(r => (r.FragranceTypeId!.Value, r.FragranceTypeName!))),
            TopNotes = topNotes
        };
    }

    private static IReadOnlyList<NamedCountDto>? TopNamed(IEnumerable<(int Id, string Name)> items)
    {
        var list = items
            .GroupBy(x => new { x.Id, x.Name })
            .Select(g => new NamedCountDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(TopN)
            .ToList();

        return list.Count == 0 ? null : list;
    }

    private static IReadOnlyList<int> DeserializeNoteIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<List<int>>(json, JsonOptions) ?? [];
    }
}
