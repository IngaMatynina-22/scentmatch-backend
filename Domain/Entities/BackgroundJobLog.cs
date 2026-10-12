namespace ScentMatch.Domain.Entities;

public class BackgroundJobLog
{
    public long Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "Running";
    public int ProcessedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? Message { get; set; }
}
