namespace ScentMatch.Domain.Entities;

public class PerfumeNote
{
    public int PerfumeId { get; set; }
    public int NoteId { get; set; }
    public string NoteType { get; set; } = string.Empty;

    public Perfume Perfume { get; set; } = null!;
    public Note Note { get; set; } = null!;
}
