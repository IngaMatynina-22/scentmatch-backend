namespace ScentMatch.Domain.Entities;

public class PerfumeSeason
{
    public int PerfumeId { get; set; }
    public int SeasonId { get; set; }
    public int? Suitability { get; set; }

    public Perfume Perfume { get; set; } = null!;
    public Season Season { get; set; } = null!;
}
