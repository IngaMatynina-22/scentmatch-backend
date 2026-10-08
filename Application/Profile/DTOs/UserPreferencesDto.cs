using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Profile.DTOs;

public class UserPreferencesDto
{
    public NamedEntityDto? PreferredSeason { get; set; }
    public NamedEntityDto? PreferredFragranceType { get; set; }
    public IReadOnlyList<NamedEntityDto> PreferredNotes { get; set; } = Array.Empty<NamedEntityDto>();

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
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateUserPreferencesRequest
{
    public int? PreferredSeasonId { get; set; }
    public int? PreferredFragranceTypeId { get; set; }
    public IReadOnlyList<int>? PreferredNoteIds { get; set; }

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
