namespace ScentMatch.Domain.Entities;

public class FavoritePerfume
{
    public int UserId { get; set; }
    public int PerfumeId { get; set; }
    public DateTime? CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
