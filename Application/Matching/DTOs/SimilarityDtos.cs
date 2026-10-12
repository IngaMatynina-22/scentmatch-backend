using ScentMatch.Application.Common.Scoring;

namespace ScentMatch.Application.Matching.DTOs;

public class PerfumeSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string FragranceTypeName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? ImageUrl { get; set; }
}

public class SimilarPerfumeDto
{
    public PerfumeSummaryDto Perfume { get; set; } = null!;
    public decimal TotalScore { get; set; }
    public IReadOnlyList<ScoreComponent> Components { get; set; } = Array.Empty<ScoreComponent>();
    public IReadOnlyList<string> PositiveReasons { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MismatchReasons { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DataGaps { get; set; } = Array.Empty<string>();
}

public class SimilarPerfumesQuery
{
    public int? Limit { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
