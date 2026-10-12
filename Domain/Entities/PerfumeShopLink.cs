namespace ScentMatch.Domain.Entities;

public class PerfumeShopLink
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Perfume Perfume { get; set; } = null!;
}
