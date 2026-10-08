using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Catalog;

public class BrandService : IBrandService
{
    private readonly ScentMatchDbContext _dbContext;

    public BrandService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _dbContext.Brands
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return brands.Select(CatalogMappings.ToDto).ToList();
    }

    public async Task<BrandDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _dbContext.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (brand is null)
        {
            throw new NotFoundException($"Brand with id {id} was not found.");
        }

        return CatalogMappings.ToDto(brand);
    }

    public async Task<BrandDto> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var nameExists = await _dbContext.Brands
            .AnyAsync(x => x.Name == request.Name, cancellationToken);

        if (nameExists)
        {
            throw new ConflictException($"Brand with name '{request.Name}' already exists.");
        }

        var brand = new Brand
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Country = request.Country,
            Website = request.Website,
            LogoUrl = request.LogoUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Brands.Add(brand);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CatalogMappings.ToDto(brand);
    }

    public async Task<BrandDto> UpdateAsync(int id, UpdateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await _dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (brand is null)
        {
            throw new NotFoundException($"Brand with id {id} was not found.");
        }

        var nameExists = await _dbContext.Brands
            .AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);

        if (nameExists)
        {
            throw new ConflictException($"Brand with name '{request.Name}' already exists.");
        }

        brand.Name = request.Name.Trim();
        brand.Description = request.Description;
        brand.Country = request.Country;
        brand.Website = request.Website;
        brand.LogoUrl = request.LogoUrl;
        brand.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(brand);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (brand is null)
        {
            throw new NotFoundException($"Brand with id {id} was not found.");
        }

        brand.IsActive = false;
        brand.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
