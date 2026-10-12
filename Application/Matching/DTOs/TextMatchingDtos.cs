namespace ScentMatch.Application.Matching.DTOs;

public class TextMatchingRequest
{
    public string Text { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 20;
}

public class ParsedTextCriteriaDto
{
    public int? SeasonId { get; set; }
    public string? SeasonName { get; set; }
    public int? FragranceTypeId { get; set; }
    public string? FragranceTypeName { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Currency { get; set; }
    public int? Intensity { get; set; }
    public string? Concentration { get; set; }
    public string? Gender { get; set; }
    public IReadOnlyList<NamedIdDto> PreferredNotes { get; set; } = Array.Empty<NamedIdDto>();
    public IReadOnlyList<NamedIdDto> ExcludedNotes { get; set; } = Array.Empty<NamedIdDto>();
}

public class NamedIdDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TextMatchingResponse
{
    public ParsedTextCriteriaDto ParsedCriteria { get; set; } = new();
    public IReadOnlyList<string> AmbiguousCriteria { get; set; } = Array.Empty<string>();
    public IReadOnlyList<MatchingResultDto> Items { get; set; } = Array.Empty<MatchingResultDto>();
}
