namespace ScentMatch.Application.History.DTOs;

public class CreateSelectionHistoryRequest
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

    public IReadOnlyList<int>? SelectedNoteIds { get; set; }
}

public class SelectionHistoryDto
{
    public int Id { get; set; }
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

    public IReadOnlyList<int> SelectedNoteIds { get; set; } = Array.Empty<int>();
    public DateTime? CreatedAt { get; set; }
}
