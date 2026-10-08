using System.ComponentModel;

namespace ScentMatch.Application.Catalog.DTOs;

public class PerfumeQueryParameters
{
    [Description("Case-insensitive search by perfume name or brand name")]
    public string? Search { get; set; }

    [Description("Filter by brand id")]
    public int? BrandId { get; set; }

    [Description("Filter by fragrance type id")]
    public int? FragranceTypeId { get; set; }

    [Description("Filter by season id")]
    public int? SeasonId { get; set; }

    /// <summary>
    /// Comma-separated note IDs (AND semantics), e.g. 1,5,8
    /// </summary>
    [Description("Comma-separated note IDs; perfume must contain all of them (AND)")]
    public string? NoteIds { get; set; }

    [Description("Minimum price (inclusive)")]
    public decimal? MinPrice { get; set; }

    [Description("Maximum price (inclusive)")]
    public decimal? MaxPrice { get; set; }

    [Description("Minimum rating 0..5 (inclusive)")]
    public decimal? MinRating { get; set; }

    [Description("Filter by gender (case-insensitive contains)")]
    public string? Gender { get; set; }

    [Description("Filter by concentration (case-insensitive contains)")]
    public string? Concentration { get; set; }

    [Description("Minimum sweetness 0..100 (inclusive)")]
    public int? MinSweetness { get; set; }

    [Description("Minimum freshness 0..100 (inclusive)")]
    public int? MinFreshness { get; set; }

    [Description("Minimum woody 0..100 (inclusive)")]
    public int? MinWoody { get; set; }

    [Description("Minimum spicy 0..100 (inclusive)")]
    public int? MinSpicy { get; set; }

    [Description("Sort field: name | price | rating | releaseYear | brand")]
    public string? SortBy { get; set; } = "name";

    [Description("Sort direction: asc | desc")]
    public string? SortOrder { get; set; } = "asc";

    [Description("Page number (1-based)")]
    public int Page { get; set; } = 1;

    [Description("Page size (1..100)")]
    public int PageSize { get; set; } = 20;

    public IReadOnlyList<int> GetNoteIds()
    {
        if (string.IsNullOrWhiteSpace(NoteIds))
        {
            return Array.Empty<int>();
        }

        return NoteIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var id) ? id : (int?)null)
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
    }
}
