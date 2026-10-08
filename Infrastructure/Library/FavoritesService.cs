using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class FavoritesService : IFavoritesService
{
    private readonly ScentMatchDbContext _dbContext;

    public FavoritesService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PerfumeListItemDto>> GetFavoritesAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FavoritePerfumes
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive);

        var totalItems = await query.CountAsync(cancellationToken);

        var perfumeIds = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.PerfumeId)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => x.PerfumeId)
            .ToListAsync(cancellationToken);

        if (perfumeIds.Count == 0)
        {
            return PagedResult<PerfumeListItemDto>.Create(
                Array.Empty<PerfumeListItemDto>(),
                pagination.Page,
                pagination.PageSize,
                totalItems);
        }

        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var perfumeMap = perfumes.ToDictionary(x => x.Id);
        var items = perfumeIds
            .Where(perfumeMap.ContainsKey)
            .Select(id => CatalogMappings.ToListItemDto(perfumeMap[id]))
            .ToList();

        return PagedResult<PerfumeListItemDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task AddFavoriteAsync(int userId, int perfumeId, CancellationToken cancellationToken = default)
    {
        var perfumeExists = await PerfumeQueryHelper.IsActivePerfumeAsync(_dbContext, perfumeId, cancellationToken);
        if (!perfumeExists)
        {
            throw new NotFoundException($"Perfume with id {perfumeId} was not found.");
        }

        var alreadyFavorite = await _dbContext.FavoritePerfumes
            .AnyAsync(x => x.UserId == userId && x.PerfumeId == perfumeId, cancellationToken);

        if (alreadyFavorite)
        {
            throw new ConflictException("Perfume is already in favorites.");
        }

        _dbContext.FavoritePerfumes.Add(new FavoritePerfume
        {
            UserId = userId,
            PerfumeId = perfumeId,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(int userId, int perfumeId, CancellationToken cancellationToken = default)
    {
        var favorite = await _dbContext.FavoritePerfumes
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PerfumeId == perfumeId, cancellationToken);

        if (favorite is null)
        {
            throw new NotFoundException("Favorite was not found.");
        }

        _dbContext.FavoritePerfumes.Remove(favorite);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
