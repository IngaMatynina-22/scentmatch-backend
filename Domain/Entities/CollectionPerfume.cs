namespace ScentMatch.Domain.Entities;

public class CollectionPerfume
{
    public int CollectionId { get; set; }
    public int PerfumeId { get; set; }
    public DateTime? AddedAt { get; set; }

    public Collection Collection { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
