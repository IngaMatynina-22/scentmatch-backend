namespace ScentMatch.Domain.Entities;

public class ViewHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PerfumeId { get; set; }
    public DateTime? ViewedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
