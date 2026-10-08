namespace ScentMatch.Domain.Entities;

public class RecommendationHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public Guid BatchId { get; set; }
    public int PerfumeId { get; set; }
    public decimal Score { get; set; }
    public string? Reasons { get; set; }
    public DateTime? CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
