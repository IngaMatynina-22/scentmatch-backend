namespace ScentMatch.Domain.Entities;

public class ScentJournalEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PerfumeId { get; set; }
    public DateOnly TestedOn { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public int? WearMinutes { get; set; }
    public string? OpeningImpression { get; set; }
    public string? HeartImpression { get; set; }
    public string? BaseImpression { get; set; }
    public string? TagsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
