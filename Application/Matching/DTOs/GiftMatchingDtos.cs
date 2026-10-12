namespace ScentMatch.Application.Matching.DTOs;

public class GiftMatchingRequest
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Currency { get; set; }
    public string? Occasion { get; set; }
    public int? SeasonId { get; set; }
    public IReadOnlyList<int>? PreferredNoteIds { get; set; }
    public IReadOnlyList<int>? ExcludedNoteIds { get; set; }
    public int? Intensity { get; set; }
    public int? FragranceTypeId { get; set; }
    public string? Gender { get; set; }
    public int MaxResults { get; set; } = 20;
}

public class GiftMatchingResponse
{
    public string? Occasion { get; set; }
    public string? Currency { get; set; }
    public IReadOnlyList<MatchingResultDto> Items { get; set; } = Array.Empty<MatchingResultDto>();
}
