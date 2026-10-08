using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Catalog;

public class SeasonService : ISeasonService
{
    private readonly ScentMatchDbContext _dbContext;

    public SeasonService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SeasonDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Seasons
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return items.Select(CatalogMappings.ToDto).ToList();
    }

    public async Task<SeasonDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Seasons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Season with id {id} was not found.");
        }

        return CatalogMappings.ToDto(entity);
    }

    public async Task<SeasonDto> CreateAsync(CreateSeasonRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);

        var entity = new Season
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        _dbContext.Seasons.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(entity);
    }

    public async Task<SeasonDto> UpdateAsync(int id, UpdateSeasonRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Seasons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Season with id {id} was not found.");
        }

        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Seasons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Season with id {id} was not found.");
        }

        var inUse = await _dbContext.PerfumeSeasons.AnyAsync(x => x.SeasonId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Season cannot be deleted because it is used by one or more perfumes.");
        }

        _dbContext.Seasons.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Seasons
            .AnyAsync(x => x.Name == name && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Season with name '{name}' already exists.");
        }
    }
}
