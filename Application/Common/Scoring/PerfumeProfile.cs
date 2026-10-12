namespace ScentMatch.Application.Common.Scoring;

/// <summary>
/// Unified perfume characteristic snapshot for matching, similarity and personalization.
/// Null numeric fields mean "unknown" — never treat as zero intensity or absence of a note.
/// </summary>
public sealed class PerfumeProfile
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public int BrandId { get; init; }
    public int FragranceTypeId { get; init; }
    public string FragranceTypeName { get; init; } = string.Empty;
    public string? Concentration { get; init; }
    public string? Gender { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public decimal? VolumeMl { get; init; }
    public int? Intensity { get; init; }
    public string? Barcode { get; init; }
    public string? ImageUrl { get; init; }

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

    public IReadOnlyList<ProfileNote> Notes { get; init; } = Array.Empty<ProfileNote>();
    public IReadOnlyList<ProfileSeason> Seasons { get; init; } = Array.Empty<ProfileSeason>();
}

public sealed record ProfileNote(int NoteId, string Name, string NoteType);

public sealed record ProfileSeason(int SeasonId, string Name, int? Suitability);

public sealed class ScoreBreakdown
{
    public decimal TotalScore { get; init; }
    public IReadOnlyList<ScoreComponent> Components { get; init; } = Array.Empty<ScoreComponent>();
    public IReadOnlyList<string> PositiveReasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MismatchReasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DataGaps { get; init; } = Array.Empty<string>();
    public bool IncompleteData { get; init; }
}

public sealed class ScoreComponent
{
    public string Key { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public decimal Similarity { get; init; }
    public decimal Contribution { get; init; }
    public bool SkippedDueToMissingData { get; init; }
}
