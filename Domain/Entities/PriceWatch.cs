namespace ScentMatch.Domain.Entities;

public class PriceWatch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PerfumeId { get; set; }
    public int? ShopLinkId { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal TargetPrice { get; set; }
    public decimal? LastKnownPrice { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public decimal? LastNotifiedPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
    public PerfumeShopLink? ShopLink { get; set; }
    public ICollection<PriceSnapshot> Snapshots { get; set; } = new List<PriceSnapshot>();
}
