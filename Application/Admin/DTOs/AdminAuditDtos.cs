namespace ScentMatch.Application.Admin.DTOs;

public class AdminAuditLogDto
{
    public long Id { get; set; }
    public int AdminUserId { get; set; }
    public string? AdminUserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Result { get; set; } = "Success";
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminAuditLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public int? AdminUserId { get; set; }
}
