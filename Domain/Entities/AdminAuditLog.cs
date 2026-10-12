namespace ScentMatch.Domain.Entities;

public class AdminAuditLog
{
    public long Id { get; set; }
    public int AdminUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Result { get; set; } = "Success";
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }

    public User AdminUser { get; set; } = null!;
}
