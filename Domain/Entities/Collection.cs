namespace ScentMatch.Domain.Entities;

public class Collection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Wardrobe scenario key (Daily, Work, …) or Custom. Null = plain collection.</summary>
    public string? ScenarioKey { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<CollectionPerfume> CollectionPerfumes { get; set; } = new List<CollectionPerfume>();
}
