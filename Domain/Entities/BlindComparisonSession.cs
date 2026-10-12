namespace ScentMatch.Domain.Entities;

public class BlindComparisonSession
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Active";
    public string? WinnerLabel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<BlindComparisonSlot> Slots { get; set; } = new List<BlindComparisonSlot>();
}

public class BlindComparisonSlot
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public int PerfumeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }

    public BlindComparisonSession Session { get; set; } = null!;
    public Perfume Perfume { get; set; } = null!;
}
