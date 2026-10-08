using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.History;

public class SelectionHistoryService : ISelectionHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ScentMatchDbContext _dbContext;

    public SelectionHistoryService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SelectionHistoryDto> CreateAsync(
        int userId,
        CreateSelectionHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(request, cancellationToken);

        var noteIds = NormalizeNoteIds(request.SelectedNoteIds);

        var entity = new SelectionHistory
        {
            UserId = userId,
            SeasonId = request.SeasonId,
            FragranceTypeId = request.FragranceTypeId,
            Sweetness = request.Sweetness,
            Freshness = request.Freshness,
            Woody = request.Woody,
            Spicy = request.Spicy,
            Citrus = request.Citrus,
            Floral = request.Floral,
            Fruity = request.Fruity,
            Powdery = request.Powdery,
            Smoky = request.Smoky,
            Oriental = request.Oriental,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            SelectedNoteIds = SerializeNoteIds(noteIds),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.SelectionHistory.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<PagedResult<SelectionHistoryDto>> GetAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SelectionHistory
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalItems = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new
            {
                x.Id,
                x.SeasonId,
                x.FragranceTypeId,
                x.Sweetness,
                x.Freshness,
                x.Woody,
                x.Spicy,
                x.Citrus,
                x.Floral,
                x.Fruity,
                x.Powdery,
                x.Smoky,
                x.Oriental,
                x.MinPrice,
                x.MaxPrice,
                x.SelectedNoteIds,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var dtos = rows.Select(x => new SelectionHistoryDto
        {
            Id = x.Id,
            SeasonId = x.SeasonId,
            FragranceTypeId = x.FragranceTypeId,
            Sweetness = x.Sweetness,
            Freshness = x.Freshness,
            Woody = x.Woody,
            Spicy = x.Spicy,
            Citrus = x.Citrus,
            Floral = x.Floral,
            Fruity = x.Fruity,
            Powdery = x.Powdery,
            Smoky = x.Smoky,
            Oriental = x.Oriental,
            MinPrice = x.MinPrice,
            MaxPrice = x.MaxPrice,
            SelectedNoteIds = DeserializeNoteIds(x.SelectedNoteIds),
            CreatedAt = x.CreatedAt
        }).ToList();

        return PagedResult<SelectionHistoryDto>.Create(dtos, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task<SelectionHistoryDto> GetByIdAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SelectionHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Selection history with id {id} was not found.");
        }

        return MapToDto(entity);
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SelectionHistory
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Selection history with id {id} was not found.");
        }

        _dbContext.SelectionHistory.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.SelectionHistory
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        _dbContext.SelectionHistory.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateForeignKeysAsync(
        CreateSelectionHistoryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SeasonId.HasValue)
        {
            var exists = await _dbContext.Seasons
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.SeasonId.Value, cancellationToken);

            if (!exists)
            {
                throw new BadRequestException($"Season with id {request.SeasonId.Value} was not found.");
            }
        }

        if (request.FragranceTypeId.HasValue)
        {
            var exists = await _dbContext.FragranceTypes
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.FragranceTypeId.Value, cancellationToken);

            if (!exists)
            {
                throw new BadRequestException($"Fragrance type with id {request.FragranceTypeId.Value} was not found.");
            }
        }

        var noteIds = NormalizeNoteIds(request.SelectedNoteIds);
        if (noteIds.Count == 0)
        {
            return;
        }

        var existingCount = await _dbContext.Notes
            .AsNoTracking()
            .CountAsync(x => noteIds.Contains(x.Id), cancellationToken);

        if (existingCount != noteIds.Count)
        {
            throw new BadRequestException("One or more selected notes were not found.");
        }
    }

    private static IReadOnlyList<int> NormalizeNoteIds(IReadOnlyList<int>? noteIds)
    {
        return noteIds is null || noteIds.Count == 0
            ? Array.Empty<int>()
            : noteIds.ToList();
    }

    private static string? SerializeNoteIds(IReadOnlyList<int> noteIds)
    {
        return noteIds.Count == 0
            ? null
            : JsonSerializer.Serialize(noteIds, JsonOptions);
    }

    private static IReadOnlyList<int> DeserializeNoteIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<List<int>>(json, JsonOptions) ?? [];
    }

    private static SelectionHistoryDto MapToDto(SelectionHistory entity) => new()
    {
        Id = entity.Id,
        SeasonId = entity.SeasonId,
        FragranceTypeId = entity.FragranceTypeId,
        Sweetness = entity.Sweetness,
        Freshness = entity.Freshness,
        Woody = entity.Woody,
        Spicy = entity.Spicy,
        Citrus = entity.Citrus,
        Floral = entity.Floral,
        Fruity = entity.Fruity,
        Powdery = entity.Powdery,
        Smoky = entity.Smoky,
        Oriental = entity.Oriental,
        MinPrice = entity.MinPrice,
        MaxPrice = entity.MaxPrice,
        SelectedNoteIds = DeserializeNoteIds(entity.SelectedNoteIds),
        CreatedAt = entity.CreatedAt
    };
}
