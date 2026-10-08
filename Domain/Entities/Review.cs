namespace ScentMatch.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PerfumeId { get; set; }
    public int Rating { get; set; }
    public string? Text { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
