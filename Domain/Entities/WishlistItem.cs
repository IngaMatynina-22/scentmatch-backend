namespace ScentMatch.Domain.Entities;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PerfumeId { get; set; }
    public string Status { get; set; } = "Wanted";
    public decimal? TargetPrice { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
