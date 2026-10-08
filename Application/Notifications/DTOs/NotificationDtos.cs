namespace ScentMatch.Application.Notifications.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int? PerfumeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UnreadNotificationCountDto
{
    public int Count { get; set; }
}

/// <summary>
/// Internal creation payload. Not exposed as a public user-facing API.
/// </summary>
public class CreateNotificationRequest
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? PerfumeId { get; set; }
}
