using ScentMatch.Application.Common.Scoring;

namespace ScentMatch.Application.Matching.DTOs;

public class MatchingRequest
{
    public int? SeasonId { get; set; }
    public int? FragranceTypeId { get; set; }

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

    public int? Intensity { get; set; }
    public string? Concentration { get; set; }
    public string? Gender { get; set; }

    public IReadOnlyList<int>? SelectedNoteIds { get; set; }
    public IReadOnlyList<int>? ExcludedNoteIds { get; set; }
}

public class MatchingResultDto
{
    public int PerfumeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal Score { get; set; }
    public IReadOnlyList<string> Reasons { get; set; } = Array.Empty<string>();

    public IReadOnlyList<ScoreComponent> Components { get; set; } = Array.Empty<ScoreComponent>();
    public IReadOnlyList<string> PositiveReasons { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MismatchReasons { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DataGaps { get; set; } = Array.Empty<string>();
}

public class MatchingResponse
{
    public IReadOnlyList<MatchingResultDto> Items { get; set; } = Array.Empty<MatchingResultDto>();
}
