using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class WishlistService : IWishlistService
{
    private readonly ScentMatchDbContext _dbContext;

    public WishlistService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<WishlistItemDto>> GetItemsAsync(
        int userId,
        WishlistQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var itemsQuery = _dbContext.WishlistItems
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = WishlistStatuses.Normalize(query.Status);
            itemsQuery = itemsQuery.Where(x => x.Status == status);
        }

        var totalItems = await itemsQuery.CountAsync(cancellationToken);

        var pageRows = await itemsQuery
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var perfumeMap = await LoadPerfumeMapAsync(
            pageRows.Select(x => x.PerfumeId).Distinct().ToList(),
            cancellationToken);

        var items = pageRows
            .Select(x => ToDto(x, perfumeMap.GetValueOrDefault(x.PerfumeId)))
            .ToList();

        return PagedResult<WishlistItemDto>.Create(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<WishlistItemDto> GetByIdAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedItemAsync(userId, id, track: false, cancellationToken);
        var perfumeMap = await LoadPerfumeMapAsync([item.PerfumeId], cancellationToken);
        return ToDto(item, perfumeMap.GetValueOrDefault(item.PerfumeId));
    }

    public async Task<WishlistItemDto> AddAsync(
        int userId,
        CreateWishlistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfumeExists = await PerfumeQueryHelper.IsActivePerfumeAsync(
            _dbContext, request.PerfumeId, cancellationToken);
        if (!perfumeExists)
        {
            throw new NotFoundException($"Perfume with id {request.PerfumeId} was not found.");
        }

        var existing = await _dbContext.WishlistItems
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.PerfumeId == request.PerfumeId,
                cancellationToken);

        if (existing is not null)
        {
            var perfumeMap = await LoadPerfumeMapAsync([existing.PerfumeId], cancellationToken);
            return ToDto(existing, perfumeMap.GetValueOrDefault(existing.PerfumeId));
        }

        var status = string.IsNullOrWhiteSpace(request.Status)
            ? WishlistStatuses.Wanted
            : WishlistStatuses.Normalize(request.Status);

        var now = DateTime.UtcNow;
        var item = new WishlistItem
        {
            UserId = userId,
            PerfumeId = request.PerfumeId,
            Status = status,
            TargetPrice = request.TargetPrice,
            Currency = NormalizeCurrency(request.Currency),
            Note = NormalizeOptional(request.Note),
            CreatedAt = now,
            UpdatedAt = now,
            PurchasedAt = status == WishlistStatuses.Purchased ? now : null
        };

        _dbContext.WishlistItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var map = await LoadPerfumeMapAsync([item.PerfumeId], cancellationToken);
        return ToDto(item, map.GetValueOrDefault(item.PerfumeId));
    }

    public async Task<WishlistItemDto> UpdateAsync(
        int userId,
        int id,
        UpdateWishlistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedItemAsync(userId, id, track: true, cancellationToken);
        var status = WishlistStatuses.Normalize(request.Status);

        item.Status = status;
        item.TargetPrice = request.TargetPrice;
        item.Currency = NormalizeCurrency(request.Currency);
        item.Note = NormalizeOptional(request.Note);
        item.UpdatedAt = DateTime.UtcNow;

        if (status == WishlistStatuses.Purchased)
        {
            item.PurchasedAt ??= DateTime.UtcNow;
        }
        else
        {
            item.PurchasedAt = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var perfumeMap = await LoadPerfumeMapAsync([item.PerfumeId], cancellationToken);
        return ToDto(item, perfumeMap.GetValueOrDefault(item.PerfumeId));
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedItemAsync(userId, id, track: true, cancellationToken);
        _dbContext.WishlistItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<WishlistItem> GetOwnedItemAsync(
        int userId,
        int id,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track
            ? _dbContext.WishlistItems.AsQueryable()
            : _dbContext.WishlistItems.AsNoTracking();

        var item = await query.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId,
            cancellationToken);

        if (item is null)
        {
            throw new NotFoundException($"Wishlist item with id {id} was not found.");
        }

        return item;
    }

    private async Task<Dictionary<int, Perfume>> LoadPerfumeMapAsync(
        IReadOnlyList<int> perfumeIds,
        CancellationToken cancellationToken)
    {
        if (perfumeIds.Count == 0)
        {
            return new Dictionary<int, Perfume>();
        }

        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return perfumes.ToDictionary(x => x.Id);
    }

    private static WishlistItemDto ToDto(WishlistItem item, Perfume? perfume)
    {
        return new WishlistItemDto
        {
            Id = item.Id,
            PerfumeId = item.PerfumeId,
            Perfume = perfume is null ? null : CatalogMappings.ToListItemDto(perfume),
            Status = item.Status,
            TargetPrice = item.TargetPrice,
            Currency = item.Currency,
            Note = item.Note,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            PurchasedAt = item.PurchasedAt
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();
    }
}
