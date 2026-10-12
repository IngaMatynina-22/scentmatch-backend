using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Common.Models;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Admin;

public class AdminAuditService : IAdminAuditService
{
    private readonly ScentMatchDbContext _dbContext;

    public AdminAuditService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        int adminUserId,
        string action,
        string entityType,
        string? entityId,
        string result = "Success",
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var entity = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = Truncate(action, 100),
            EntityType = Truncate(entityType, 100),
            EntityId = entityId is null ? null : Truncate(entityId, 100),
            Result = Truncate(string.IsNullOrWhiteSpace(result) ? "Success" : result, 30),
            Details = SanitizeDetails(details),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AdminAuditLogs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminAuditLogDto>> GetAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.AdminAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            q = q.Where(x => x.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            q = q.Where(x => x.EntityType == entityType);
        }

        if (query.AdminUserId.HasValue)
        {
            q = q.Where(x => x.AdminUserId == query.AdminUserId.Value);
        }

        var totalItems = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminAuditLogDto
            {
                Id = x.Id,
                AdminUserId = x.AdminUserId,
                AdminUserName = x.AdminUser.UserName,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Result = x.Result,
                Details = x.Details,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<AdminAuditLogDto>.Create(items, query.Page, query.PageSize, totalItems);
    }

    private static string? SanitizeDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        var cleaned = details.Trim();
        foreach (var marker in new[] { "password", "token", "apikey", "api_key", "secret", "authorization", "journal" })
        {
            if (cleaned.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return "Details redacted.";
            }
        }

        return Truncate(cleaned, 4000);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
