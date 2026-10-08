using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Catalog;

public class FragranceTypeService : IFragranceTypeService
{
    private readonly ScentMatchDbContext _dbContext;

    public FragranceTypeService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FragranceTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.FragranceTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items.Select(CatalogMappings.ToDto).ToList();
    }

    public async Task<FragranceTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FragranceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Fragrance type with id {id} was not found.");
        }

        return CatalogMappings.ToDto(entity);
    }

    public async Task<FragranceTypeDto> CreateAsync(CreateFragranceTypeRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueNameAsync(request.Name, excludeId: null, cancellationToken);

        var entity = new FragranceType
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        _dbContext.FragranceTypes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(entity);
    }

    public async Task<FragranceTypeDto> UpdateAsync(int id, UpdateFragranceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FragranceTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Fragrance type with id {id} was not found.");
        }

        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FragranceTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Fragrance type with id {id} was not found.");
        }

        var inUse = await _dbContext.Perfumes.AnyAsync(x => x.FragranceTypeId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Fragrance type cannot be deleted because it is used by one or more perfumes.");
        }

        _dbContext.FragranceTypes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.FragranceTypes
            .AnyAsync(x => x.Name == name && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Fragrance type with name '{name}' already exists.");
        }
    }
}
