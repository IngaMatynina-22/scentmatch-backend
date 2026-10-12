using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Admin.Interfaces;

public interface IAdminAuditService
{
    Task LogAsync(
        int adminUserId,
        string action,
        string entityType,
        string? entityId,
        string result = "Success",
        string? details = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminAuditLogDto>> GetAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default);
}
