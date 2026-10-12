using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.History;

public class SelectionHistoryService : ISelectionHistoryService
{
    private const int MaxStoredResults = 20;
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
        var entity = BuildEntity(userId, request, noteIds, resultsJson: null, scenarioType: MatchingScenarioTypes.Personal);

        _dbContext.SelectionHistory.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public Task<SelectionHistoryDto> CreateFromMatchingAsync(
        int userId,
        MatchingRequest request,
        IReadOnlyList<MatchingResultDto> results,
        CancellationToken cancellationToken = default) =>
        CreateFromMatchingAsync(
            userId,
            request,
            results,
            MatchingScenarioTypes.Personal,
            cancellationToken);

    public async Task<SelectionHistoryDto> CreateFromMatchingAsync(
        int userId,
        MatchingRequest request,
        IReadOnlyList<MatchingResultDto> results,
        string scenarioType,
        CancellationToken cancellationToken = default)
    {
        var normalizedScenario = MatchingScenarioTypes.IsValid(scenarioType)
            ? MatchingScenarioTypes.Normalize(scenarioType)
            : MatchingScenarioTypes.Personal;

        var createRequest = new CreateSelectionHistoryRequest
        {
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
            SelectedNoteIds = request.SelectedNoteIds
        };

        await ValidateForeignKeysAsync(createRequest, cancellationToken);

        var noteIds = NormalizeNoteIds(createRequest.SelectedNoteIds);
        var resultItems = results
            .Take(MaxStoredResults)
            .Select(x => new SelectionHistoryResultItemDto
            {
                PerfumeId = x.PerfumeId,
                Name = x.Name,
                BrandName = x.BrandName,
                Price = x.Price,
                Score = x.Score,
                Reasons = x.Reasons
            })
            .ToList();

        var entity = BuildEntity(
            userId,
            createRequest,
            noteIds,
            resultsJson: resultItems.Count == 0
                ? null
                : JsonSerializer.Serialize(resultItems, JsonOptions),
            scenarioType: normalizedScenario);

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
            .ToListAsync(cancellationToken);

        var dtos = rows.Select(MapToDto).ToList();
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

    private static SelectionHistory BuildEntity(
        int userId,
        CreateSelectionHistoryRequest request,
        IReadOnlyList<int> noteIds,
        string? resultsJson,
        string scenarioType) => new()
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
        ResultsJson = resultsJson,
        ScenarioType = scenarioType,
        CreatedAt = DateTime.UtcNow
    };

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
            : noteIds.Distinct().ToList();
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

    private static IReadOnlyList<SelectionHistoryResultItemDto> DeserializeResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SelectionHistoryResultItemDto>();
        }

        return JsonSerializer.Deserialize<List<SelectionHistoryResultItemDto>>(json, JsonOptions)
               ?? [];
    }

    private static SelectionHistoryDto MapToDto(SelectionHistory entity) => new()
    {
        Id = entity.Id,
        ScenarioType = entity.ScenarioType,
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
        Results = DeserializeResults(entity.ResultsJson),
        CreatedAt = entity.CreatedAt
    };
}
