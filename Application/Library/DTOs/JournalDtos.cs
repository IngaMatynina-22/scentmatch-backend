using System.ComponentModel;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Library.DTOs;

public class JournalEntryDto
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public PerfumeListItemDto? Perfume { get; set; }
    public DateOnly TestedOn { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    /// <summary>Subjective wear duration in minutes. Not an objective longevity claim.</summary>
    public int? WearMinutes { get; set; }
    public string? OpeningImpression { get; set; }
    public string? HeartImpression { get; set; }
    public string? BaseImpression { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateJournalEntryRequest
{
    public int PerfumeId { get; set; }
    public DateOnly TestedOn { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public int? WearMinutes { get; set; }
    public string? OpeningImpression { get; set; }
    public string? HeartImpression { get; set; }
    public string? BaseImpression { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}

public class UpdateJournalEntryRequest
{
    public DateOnly TestedOn { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public int? WearMinutes { get; set; }
    public string? OpeningImpression { get; set; }
    public string? HeartImpression { get; set; }
    public string? BaseImpression { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}

public class JournalQueryParameters : PaginationQuery
{
    [Description("Filter by perfume id")]
    public int? PerfumeId { get; set; }

    [Description("Filter by rating (1-10)")]
    public int? Rating { get; set; }

    [Description("Filter testedOn from (inclusive)")]
    public DateOnly? From { get; set; }

    [Description("Filter testedOn to (inclusive)")]
    public DateOnly? To { get; set; }
}
