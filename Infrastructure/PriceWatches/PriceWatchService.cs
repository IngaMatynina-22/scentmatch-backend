using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.PriceWatches.DTOs;
using ScentMatch.Application.PriceWatches.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.PriceWatches;

public class PriceWatchService : IPriceWatchService
{
    private readonly ScentMatchDbContext _dbContext;

    public PriceWatchService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PriceWatchDto> CreateAsync(
        int userId,
        CreatePriceWatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfume = await _dbContext.Perfumes
            .AsNoTracking()
            .Include(x => x.Brand)
            .FirstOrDefaultAsync(x => x.Id == request.PerfumeId && x.IsActive, cancellationToken);

        if (perfume is null)
        {
            throw new NotFoundException($"Perfume with id {request.PerfumeId} was not found.");
        }

        if (request.ShopLinkId.HasValue)
        {
            var shopLinkExists = await _dbContext.PerfumeShopLinks
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == request.ShopLinkId.Value && x.PerfumeId == request.PerfumeId,
                    cancellationToken);

            if (!shopLinkExists)
            {
                throw new BadRequestException(
                    $"Shop link {request.ShopLinkId.Value} does not belong to perfume {request.PerfumeId}.");
            }
        }

        var now = DateTime.UtcNow;
        var entity = new PriceWatch
        {
            UserId = userId,
            PerfumeId = request.PerfumeId,
            ShopLinkId = request.ShopLinkId,
            SourceName = string.IsNullOrWhiteSpace(request.SourceName) ? null : request.SourceName.Trim(),
            SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            TargetPrice = request.TargetPrice,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.PriceWatches.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(entity.Id, userId, cancellationToken)
               ?? throw new NotFoundException($"Price watch with id {entity.Id} was not found.");
    }

    public async Task<PagedResult<PriceWatchDto>> ListAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PriceWatches
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new PriceWatchDto
            {
                Id = x.Id,
                PerfumeId = x.PerfumeId,
                PerfumeName = x.Perfume.Name,
                BrandName = x.Perfume.Brand.Name,
                ShopLinkId = x.ShopLinkId,
                SourceName = x.SourceName,
                SourceUrl = x.SourceUrl,
                Currency = x.Currency,
                TargetPrice = x.TargetPrice,
                LastKnownPrice = x.LastKnownPrice,
                LastCheckedAt = x.LastCheckedAt,
                LastNotifiedAt = x.LastNotifiedAt,
                LastNotifiedPrice = x.LastNotifiedPrice,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PriceWatchDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task<PriceWatchDto> GetAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var dto = await MapAsync(id, userId, cancellationToken);
        if (dto is null)
        {
            throw new NotFoundException($"Price watch with id {id} was not found.");
        }

        return dto;
    }

    public async Task<PriceWatchDto> UpdateAsync(
        int userId,
        int id,
        UpdatePriceWatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PriceWatches
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Price watch with id {id} was not found.");
        }

        if (request.ShopLinkId.HasValue)
        {
            var shopLinkExists = await _dbContext.PerfumeShopLinks
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == request.ShopLinkId.Value && x.PerfumeId == entity.PerfumeId,
                    cancellationToken);

            if (!shopLinkExists)
            {
                throw new BadRequestException(
                    $"Shop link {request.ShopLinkId.Value} does not belong to perfume {entity.PerfumeId}.");
            }

            entity.ShopLinkId = request.ShopLinkId;
        }

        if (request.TargetPrice.HasValue)
        {
            entity.TargetPrice = request.TargetPrice.Value;
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            entity.Currency = request.Currency.Trim().ToUpperInvariant();
        }

        if (request.SourceUrl is not null)
        {
            entity.SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim();
        }

        if (request.SourceName is not null)
        {
            entity.SourceName = string.IsNullOrWhiteSpace(request.SourceName) ? null : request.SourceName.Trim();
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(id, userId, cancellationToken)
               ?? throw new NotFoundException($"Price watch with id {id} was not found.");
    }

    public async Task DeleteAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PriceWatches
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Price watch with id {id} was not found.");
        }

        _dbContext.PriceWatches.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceSnapshotDto>> GetHistoryAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.PriceWatches
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"Price watch with id {id} was not found.");
        }

        return await _dbContext.PriceSnapshots
            .AsNoTracking()
            .Where(x => x.PriceWatchId == id)
            .OrderByDescending(x => x.CapturedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new PriceSnapshotDto
            {
                Id = x.Id,
                PerfumeId = x.PerfumeId,
                PriceWatchId = x.PriceWatchId,
                SourceName = x.SourceName,
                SourceUrl = x.SourceUrl,
                Currency = x.Currency,
                Price = x.Price,
                CapturedAt = x.CapturedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<PriceWatchDto?> MapAsync(int id, int userId, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceWatches
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new PriceWatchDto
            {
                Id = x.Id,
                PerfumeId = x.PerfumeId,
                PerfumeName = x.Perfume.Name,
                BrandName = x.Perfume.Brand.Name,
                ShopLinkId = x.ShopLinkId,
                SourceName = x.SourceName,
                SourceUrl = x.SourceUrl,
                Currency = x.Currency,
                TargetPrice = x.TargetPrice,
                LastKnownPrice = x.LastKnownPrice,
                LastCheckedAt = x.LastCheckedAt,
                LastNotifiedAt = x.LastNotifiedAt,
                LastNotifiedPrice = x.LastNotifiedPrice,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
