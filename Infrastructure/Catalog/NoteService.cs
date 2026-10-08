using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Catalog;

public class NoteService : INoteService
{
    private readonly ScentMatchDbContext _dbContext;

    public NoteService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Notes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items.Select(CatalogMappings.ToDto).ToList();
    }

    public async Task<NoteDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Note with id {id} was not found.");
        }

        return CatalogMappings.ToDto(entity);
    }

    public async Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);

        var entity = new Note
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Category = request.Category
        };

        _dbContext.Notes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(entity);
    }

    public async Task<NoteDto> UpdateAsync(int id, UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Note with id {id} was not found.");
        }

        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        entity.Category = request.Category;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMappings.ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Note with id {id} was not found.");
        }

        var inUse = await _dbContext.PerfumeNotes.AnyAsync(x => x.NoteId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Note cannot be deleted because it is used by one or more perfumes.");
        }

        _dbContext.Notes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Notes
            .AnyAsync(x => x.Name == name && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Note with name '{name}' already exists.");
        }
    }
}
