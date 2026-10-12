using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Common.Models;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Admin;

public class AdminJobsService : IAdminJobsService
{
    private readonly ScentMatchDbContext _dbContext;

    public AdminJobsService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BackgroundJobLogDto>> GetRecentAsync(
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BackgroundJobLogs.AsNoTracking();
        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new BackgroundJobLogDto
            {
                Id = x.Id,
                JobName = x.JobName,
                StartedAt = x.StartedAt,
                FinishedAt = x.FinishedAt,
                Status = x.Status,
                ProcessedCount = x.ProcessedCount,
                ErrorCount = x.ErrorCount,
                Message = x.Message
            })
            .ToListAsync(cancellationToken);

        return PagedResult<BackgroundJobLogDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }
}
