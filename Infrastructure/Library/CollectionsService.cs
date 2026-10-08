using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class CollectionsService : ICollectionsService
{
    private readonly ScentMatchDbContext _dbContext;

    public CollectionsService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<CollectionDto>> GetCollectionsAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenBy(x => x.Name)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new CollectionDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                PerfumeCount = x.CollectionPerfumes.Count(cp => cp.Perfume.IsActive && cp.Perfume.Brand.IsActive),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<CollectionDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task<CollectionDetailsDto> GetByIdAsync(
        int userId,
        int collectionId,
        CancellationToken cancellationToken = default)
    {
        var collection = await GetOwnedCollectionQuery(userId, collectionId)
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException($"Collection with id {collectionId} was not found.");
        }

        var perfumeIds = await _dbContext.CollectionPerfumes
            .AsNoTracking()
            .Where(x => x.CollectionId == collectionId
                        && x.Perfume.IsActive
                        && x.Perfume.Brand.IsActive)
            .OrderByDescending(x => x.AddedAt)
            .ThenBy(x => x.PerfumeId)
            .Select(x => x.PerfumeId)
            .ToListAsync(cancellationToken);

        var perfumes = perfumeIds.Count == 0
            ? []
            : await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
                .Where(x => perfumeIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

        var perfumeMap = perfumes.ToDictionary(x => x.Id);
        var perfumeDtos = perfumeIds
            .Where(perfumeMap.ContainsKey)
            .Select(id => CatalogMappings.ToListItemDto(perfumeMap[id]))
            .ToList();

        return new CollectionDetailsDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            PerfumeCount = perfumeDtos.Count,
            Perfumes = perfumeDtos,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    public async Task<CollectionDto> CreateAsync(
        int userId,
        CreateCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var collection = new Collection
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            PerfumeCount = 0,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    public async Task<CollectionDto> UpdateAsync(
        int userId,
        int collectionId,
        UpdateCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var collection = await GetOwnedCollectionQuery(userId, collectionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException($"Collection with id {collectionId} was not found.");
        }

        collection.Name = request.Name.Trim();
        collection.Description = NormalizeOptional(request.Description);
        collection.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var perfumeCount = await _dbContext.CollectionPerfumes
            .CountAsync(
                x => x.CollectionId == collectionId
                     && x.Perfume.IsActive
                     && x.Perfume.Brand.IsActive,
                cancellationToken);

        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            PerfumeCount = perfumeCount,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    public async Task DeleteAsync(int userId, int collectionId, CancellationToken cancellationToken = default)
    {
        var collection = await GetOwnedCollectionQuery(userId, collectionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException($"Collection with id {collectionId} was not found.");
        }

        _dbContext.Collections.Remove(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPerfumeAsync(
        int userId,
        int collectionId,
        int perfumeId,
        CancellationToken cancellationToken = default)
    {
        var collectionExists = await GetOwnedCollectionQuery(userId, collectionId)
            .AnyAsync(cancellationToken);

        if (!collectionExists)
        {
            throw new NotFoundException($"Collection with id {collectionId} was not found.");
        }

        var perfumeExists = await PerfumeQueryHelper.IsActivePerfumeAsync(_dbContext, perfumeId, cancellationToken);
        if (!perfumeExists)
        {
            throw new NotFoundException($"Perfume with id {perfumeId} was not found.");
        }

        var alreadyAdded = await _dbContext.CollectionPerfumes
            .AnyAsync(x => x.CollectionId == collectionId && x.PerfumeId == perfumeId, cancellationToken);

        if (alreadyAdded)
        {
            throw new ConflictException("Perfume is already in this collection.");
        }

        _dbContext.CollectionPerfumes.Add(new CollectionPerfume
        {
            CollectionId = collectionId,
            PerfumeId = perfumeId,
            AddedAt = DateTime.UtcNow
        });

        var collection = await GetOwnedCollectionQuery(userId, collectionId)
            .FirstAsync(cancellationToken);
        collection.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePerfumeAsync(
        int userId,
        int collectionId,
        int perfumeId,
        CancellationToken cancellationToken = default)
    {
        var collectionExists = await GetOwnedCollectionQuery(userId, collectionId)
            .AnyAsync(cancellationToken);

        if (!collectionExists)
        {
            throw new NotFoundException($"Collection with id {collectionId} was not found.");
        }

        var relation = await _dbContext.CollectionPerfumes
            .FirstOrDefaultAsync(
                x => x.CollectionId == collectionId && x.PerfumeId == perfumeId,
                cancellationToken);

        if (relation is null)
        {
            throw new NotFoundException("Perfume was not found in this collection.");
        }

        _dbContext.CollectionPerfumes.Remove(relation);

        var collection = await GetOwnedCollectionQuery(userId, collectionId)
            .FirstAsync(cancellationToken);
        collection.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Collection> GetOwnedCollectionQuery(int userId, int collectionId)
    {
        return _dbContext.Collections
            .Where(x => x.Id == collectionId && x.UserId == userId);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
