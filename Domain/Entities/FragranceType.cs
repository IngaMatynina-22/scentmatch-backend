namespace ScentMatch.Domain.Entities;

public class FragranceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Perfume> Perfumes { get; set; } = new List<Perfume>();
    public ICollection<UserPreferences> UserPreferences { get; set; } = new List<UserPreferences>();
    public ICollection<SelectionHistory> SelectionHistory { get; set; } = new List<SelectionHistory>();
}
