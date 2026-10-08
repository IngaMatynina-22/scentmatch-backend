using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.Library;

namespace ScentMatch.Infrastructure.History;

public class ViewHistoryService : IViewHistoryService
{
    private readonly ScentMatchDbContext _dbContext;

    public ViewHistoryService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ViewHistoryDto>> GetAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ViewHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive);

        var totalItems = await query.CountAsync(cancellationToken);

        var pageRows = await query
            .OrderByDescending(x => x.ViewedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new { x.Id, x.PerfumeId, x.ViewedAt })
            .ToListAsync(cancellationToken);

        if (pageRows.Count == 0)
        {
            return PagedResult<ViewHistoryDto>.Create(
                Array.Empty<ViewHistoryDto>(),
                pagination.Page,
                pagination.PageSize,
                totalItems);
        }

        var perfumeIds = pageRows.Select(x => x.PerfumeId).Distinct().ToList();
        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var perfumeMap = perfumes.ToDictionary(x => x.Id);
        var items = pageRows
            .Where(row => perfumeMap.ContainsKey(row.PerfumeId))
            .Select(row => new ViewHistoryDto
            {
                Id = row.Id,
                ViewedAt = row.ViewedAt,
                Perfume = CatalogMappings.ToListItemDto(perfumeMap[row.PerfumeId])
            })
            .ToList();

        return PagedResult<ViewHistoryDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task RegisterViewAsync(int userId, int perfumeId, CancellationToken cancellationToken = default)
    {
        var perfumeExists = await PerfumeQueryHelper.IsActivePerfumeAsync(_dbContext, perfumeId, cancellationToken);
        if (!perfumeExists)
        {
            throw new NotFoundException($"Perfume with id {perfumeId} was not found.");
        }

        var existing = await _dbContext.ViewHistory
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PerfumeId == perfumeId, cancellationToken);

        if (existing is not null)
        {
            existing.ViewedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var entry = new ViewHistory
        {
            UserId = userId,
            PerfumeId = perfumeId,
            ViewedAt = DateTime.UtcNow
        };

        _dbContext.ViewHistory.Add(entry);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(entry).State = EntityState.Detached;

            var concurrent = await _dbContext.ViewHistory
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PerfumeId == perfumeId, cancellationToken);

            if (concurrent is null)
            {
                throw;
            }

            concurrent.ViewedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(int userId, int historyId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.ViewHistory
            .FirstOrDefaultAsync(x => x.Id == historyId && x.UserId == userId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("View history item was not found.");
        }

        _dbContext.ViewHistory.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.ViewHistory
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        _dbContext.ViewHistory.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

