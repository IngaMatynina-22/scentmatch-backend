using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Analytics.DTOs;

public class PreferenceAnalyticsDto
{
    public ExplicitPreferencesSection? ExplicitPreferences { get; set; }
    public RatingSignalsSection? ReviewRatings { get; set; }
    public RatingSignalsSection? JournalRatings { get; set; }
    public FavoritesSection? Favorites { get; set; }
    public CollectionsSection? Collections { get; set; }
    public SelectionHistorySection? PersonalSelectionHistory { get; set; }
    public PersonalizationStateDto Personalization { get; set; } = new();
}

public class ExplicitPreferencesSection
{
    public NamedEntityDto? PreferredSeason { get; set; }
    public NamedEntityDto? PreferredFragranceType { get; set; }
    public IReadOnlyList<NamedEntityDto>? PreferredNotes { get; set; }
    public int? Sweetness { get; set; }
    public int? Freshness { get; set; }
    public int? Woody { get; set; }
    public int? Spicy { get; set; }
    public int? Citrus { get; set; }
    public int? Floral { get; set; }
    public int? Fruity { get; set; }
    public int? Powdery { get; set; }
    public int? Smoky { get; set; }
    public int? Oriental { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

public class RatingSignalsSection
{
    public int RatedCount { get; set; }
    public decimal? AverageRating { get; set; }
    public IReadOnlyList<NamedCountDto>? TopNotes { get; set; }
    public IReadOnlyList<NamedCountDto>? TopFragranceTypes { get; set; }
}

public class FavoritesSection
{
    public int Count { get; set; }
    public IReadOnlyList<NamedCountDto>? TopNotes { get; set; }
    public IReadOnlyList<NamedCountDto>? TopFragranceTypes { get; set; }
}

public class CollectionsSection
{
    public int TotalCollections { get; set; }
    public int TotalPerfumes { get; set; }
    public IReadOnlyList<NamedCountDto>? CountsByScenario { get; set; }
}

public class SelectionHistorySection
{
    public int Count { get; set; }
    public IReadOnlyList<NamedCountDto>? TopSeasons { get; set; }
    public IReadOnlyList<NamedCountDto>? TopFragranceTypes { get; set; }
    public IReadOnlyList<NamedCountDto>? TopNotes { get; set; }
}

public class NamedCountDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PersonalizationStateDto
{
    public bool UseBehavioralSignals { get; set; } = true;
    public DateTime? PersonalizationResetAt { get; set; }
}

public class ResetPersonalizationRequest
{
    public bool? UseBehavioralSignals { get; set; }
}

public class ResetPersonalizationResponse
{
    public DateTime PersonalizationResetAt { get; set; }
    public bool UseBehavioralSignals { get; set; }
}
