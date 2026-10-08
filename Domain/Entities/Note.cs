namespace ScentMatch.Domain.Entities;

public class Note
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }

    public ICollection<PerfumeNote> PerfumeNotes { get; set; } = new List<PerfumeNote>();
}
