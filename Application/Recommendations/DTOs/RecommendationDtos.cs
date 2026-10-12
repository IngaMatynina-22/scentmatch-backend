using ScentMatch.Application.Common.Scoring;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Recommendations.DTOs;

public class RecommendationDto
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

    public static RecommendationDto FromMatching(MatchingResultDto item) => new()
    {
        PerfumeId = item.PerfumeId,
        Name = item.Name,
        BrandName = item.BrandName,
        Price = item.Price,
        Score = item.Score,
        Reasons = item.Reasons.Count > 0 ? item.Reasons : item.PositiveReasons,
        Components = item.Components,
        PositiveReasons = item.PositiveReasons.Count > 0 ? item.PositiveReasons : item.Reasons,
        MismatchReasons = item.MismatchReasons,
        DataGaps = item.DataGaps
    };
}

public class RecommendationsResponse
{
    public bool PreferencesConfigured { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<RecommendationDto> Items { get; set; } = Array.Empty<RecommendationDto>();
}

public class RecommendationHistorySummaryDto
{
    public Guid BatchId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal? TopScore { get; set; }
}

public class RecommendationHistoryDetailsDto
{
    public Guid BatchId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public IReadOnlyList<RecommendationDto> Items { get; set; } = Array.Empty<RecommendationDto>();
}
