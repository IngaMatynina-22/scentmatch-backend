namespace ScentMatch.Domain.Entities;

public class PriceSnapshot
{
    public long Id { get; set; }
    public int PerfumeId { get; set; }
    public int? PriceWatchId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal Price { get; set; }
    public DateTime CapturedAt { get; set; }

    public Perfume Perfume { get; set; } = null!;
    public PriceWatch? PriceWatch { get; set; }
}
