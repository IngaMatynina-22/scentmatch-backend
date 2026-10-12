using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class WardrobeService : IWardrobeService
{
    private readonly ScentMatchDbContext _dbContext;
    private readonly ICollectionsService _collectionsService;

    public WardrobeService(ScentMatchDbContext dbContext, ICollectionsService collectionsService)
    {
        _dbContext = dbContext;
        _collectionsService = collectionsService;
    }

    public async Task<WardrobeResponse> GetWardrobeAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var collections = await _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ScenarioKey != null)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.ScenarioKey
            })
            .ToListAsync(cancellationToken);

        var collectionIds = collections.Select(x => x.Id).ToList();
        var relations = collectionIds.Count == 0
            ? []
            : await _dbContext.CollectionPerfumes
                .AsNoTracking()
                .Where(x => collectionIds.Contains(x.CollectionId)
                            && x.Perfume.IsActive
                            && x.Perfume.Brand.IsActive)
                .OrderByDescending(x => x.AddedAt)
                .ThenBy(x => x.PerfumeId)
                .Select(x => new { x.CollectionId, x.PerfumeId })
                .ToListAsync(cancellationToken);

        var perfumeIds = relations.Select(x => x.PerfumeId).Distinct().ToList();
        var perfumeMap = perfumeIds.Count == 0
            ? new Dictionary<int, Perfume>()
            : (await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
                .Where(x => perfumeIds.Contains(x.Id))
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        var relationsByCollection = relations
            .GroupBy(x => x.CollectionId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.PerfumeId).ToList());

        var scenarios = WardrobeScenarios.All
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(scenarioKey =>
            {
                var scenarioCollections = collections
                    .Where(c => string.Equals(c.ScenarioKey, scenarioKey, StringComparison.OrdinalIgnoreCase))
                    .Select(c =>
                    {
                        var ids = relationsByCollection.GetValueOrDefault(c.Id) ?? [];
                        var perfumes = ids
                            .Where(perfumeMap.ContainsKey)
                            .Select(id => CatalogMappings.ToListItemDto(perfumeMap[id]))
                            .ToList();

                        return new WardrobeCollectionDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Description = c.Description,
                            Perfumes = perfumes
                        };
                    })
                    .ToList();

                return new WardrobeScenarioGroupDto
                {
                    ScenarioKey = scenarioKey,
                    PerfumeCount = scenarioCollections.Sum(c => c.Perfumes.Count),
                    Collections = scenarioCollections
                };
            })
            .ToList();

        return new WardrobeResponse { Scenarios = scenarios };
    }

    public async Task<WardrobeStatsDto> GetStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ScenarioKey != null)
            .Select(x => new
            {
                x.ScenarioKey,
                PerfumeCount = x.CollectionPerfumes.Count(cp =>
                    cp.Perfume.IsActive && cp.Perfume.Brand.IsActive)
            })
            .ToListAsync(cancellationToken);

        var byScenario = rows
            .GroupBy(x => WardrobeScenarios.Normalize(x.ScenarioKey!))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    CollectionCount = g.Count(),
                    PerfumeCount = g.Sum(x => x.PerfumeCount)
                });

        var scenarios = WardrobeScenarios.All
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(key =>
            {
                byScenario.TryGetValue(key, out var stats);
                return new WardrobeScenarioStatDto
                {
                    ScenarioKey = key,
                    CollectionCount = stats?.CollectionCount ?? 0,
                    PerfumeCount = stats?.PerfumeCount ?? 0
                };
            })
            .ToList();

        return new WardrobeStatsDto { Scenarios = scenarios };
    }

    public async Task AssignAsync(
        int userId,
        AssignWardrobeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scenarioKey = WardrobeScenarios.Normalize(request.ScenarioKey);

        var perfumeExists = await PerfumeQueryHelper.IsActivePerfumeAsync(
            _dbContext, request.PerfumeId, cancellationToken);
        if (!perfumeExists)
        {
            throw new NotFoundException($"Perfume with id {request.PerfumeId} was not found.");
        }

        var collection = await _dbContext.Collections
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ScenarioKey == scenarioKey,
                cancellationToken);

        if (collection is null)
        {
            var name = string.IsNullOrWhiteSpace(request.CollectionName)
                ? scenarioKey
                : request.CollectionName.Trim();

            var created = await _collectionsService.CreateAsync(
                userId,
                new CreateCollectionRequest
                {
                    Name = name,
                    ScenarioKey = scenarioKey
                },
                cancellationToken);

            collection = await _dbContext.Collections
                .FirstAsync(x => x.Id == created.Id && x.UserId == userId, cancellationToken);
        }

        var alreadyAdded = await _dbContext.CollectionPerfumes
            .AnyAsync(
                x => x.CollectionId == collection.Id && x.PerfumeId == request.PerfumeId,
                cancellationToken);

        if (alreadyAdded)
        {
            return;
        }

        await _collectionsService.AddPerfumeAsync(
            userId, collection.Id, request.PerfumeId, cancellationToken);
    }

    public async Task RemovePerfumeAsync(
        int userId,
        string scenarioKey,
        int perfumeId,
        CancellationToken cancellationToken = default)
    {
        if (!WardrobeScenarios.IsValid(scenarioKey))
        {
            throw new BadRequestException(
                $"ScenarioKey must be one of: {WardrobeScenarios.AllowedList}.");
        }

        var normalized = WardrobeScenarios.Normalize(scenarioKey);

        var collection = await _dbContext.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ScenarioKey == normalized,
                cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException(
                $"Wardrobe scenario '{normalized}' collection was not found.");
        }

        await _collectionsService.RemovePerfumeAsync(
            userId, collection.Id, perfumeId, cancellationToken);
    }
}
