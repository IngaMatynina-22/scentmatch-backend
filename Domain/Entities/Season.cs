namespace ScentMatch.Domain.Entities;

public class Season
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<PerfumeSeason> PerfumeSeasons { get; set; } = new List<PerfumeSeason>();
    public ICollection<UserPreferences> UserPreferences { get; set; } = new List<UserPreferences>();
    public ICollection<SelectionHistory> SelectionHistory { get; set; } = new List<SelectionHistory>();
}
