namespace ScentMatch.Domain.Entities;

public class SelectionHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
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

    public string? SelectedNoteIds { get; set; }

    /// <summary>
    /// JSON array of top matching results (perfumeId, name, score, reasons).
    /// </summary>
    public string? ResultsJson { get; set; }

    public DateTime? CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Season? Season { get; set; }
    public FragranceType? FragranceType { get; set; }
}
