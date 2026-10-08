using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Catalog;

public class PerfumeService : IPerfumeService
{
    private readonly ScentMatchDbContext _dbContext;

    public PerfumeService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PerfumeListItemDto>> GetAllAsync(
        PerfumeQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var filtered = ApplyFilters(QueryActivePerfumesBase(), query);

        var totalItems = await filtered.CountAsync(cancellationToken);

        var sorted = ApplySort(filtered, query.SortBy, query.SortOrder);

        var pageItems = await sorted
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(x => x.Brand)
            .Include(x => x.FragranceType)
            .Include(x => x.PerfumeNotes)
                .ThenInclude(x => x.Note)
            .Include(x => x.PerfumeSeasons)
                .ThenInclude(x => x.Season)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var items = pageItems.Select(CatalogMappings.ToListItemDto).ToList();
        return PagedResult<PerfumeListItemDto>.Create(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<PerfumeDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var perfume = await QueryActivePerfumes()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (perfume is null)
        {
            throw new NotFoundException($"Perfume with id {id} was not found.");
        }

        return CatalogMappings.ToDetailsDto(perfume);
    }

    public async Task<PerfumeDetailsDto> CreateAsync(CreatePerfumeRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRelationsAsync(request.BrandId, request.FragranceTypeId, request.Notes, request.Seasons, cancellationToken);

        return await ExecuteInTransactionAsync(async () =>
        {
            var perfume = new Perfume
            {
                Name = request.Name.Trim(),
                BrandId = request.BrandId,
                FragranceTypeId = request.FragranceTypeId,
                Description = request.Description,
                ReleaseYear = request.ReleaseYear,
                Concentration = request.Concentration,
                Gender = request.Gender,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                Rating = 0,
                ReviewsCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            CatalogMappings.ApplyCharacteristics(perfume, request.Characteristics);

            _dbContext.Perfumes.Add(perfume);
            await _dbContext.SaveChangesAsync(cancellationToken);

            AddNotes(perfume.Id, request.Notes);
            AddSeasons(perfume.Id, request.Seasons);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(perfume.Id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<PerfumeDetailsDto> UpdateAsync(int id, UpdatePerfumeRequest request, CancellationToken cancellationToken = default)
    {
        var perfume = await _dbContext.Perfumes
            .Include(x => x.PerfumeNotes)
            .Include(x => x.PerfumeSeasons)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (perfume is null)
        {
            throw new NotFoundException($"Perfume with id {id} was not found.");
        }

        await ValidateRelationsAsync(request.BrandId, request.FragranceTypeId, request.Notes, request.Seasons, cancellationToken);

        return await ExecuteInTransactionAsync(async () =>
        {
            perfume.Name = request.Name.Trim();
            perfume.BrandId = request.BrandId;
            perfume.FragranceTypeId = request.FragranceTypeId;
            perfume.Description = request.Description;
            perfume.ReleaseYear = request.ReleaseYear;
            perfume.Concentration = request.Concentration;
            perfume.Gender = request.Gender;
            perfume.Price = request.Price;
            perfume.ImageUrl = request.ImageUrl;
            perfume.UpdatedAt = DateTime.UtcNow;
            CatalogMappings.ApplyCharacteristics(perfume, request.Characteristics);

            _dbContext.PerfumeNotes.RemoveRange(perfume.PerfumeNotes);
            _dbContext.PerfumeSeasons.RemoveRange(perfume.PerfumeSeasons);
            await _dbContext.SaveChangesAsync(cancellationToken);

            AddNotes(perfume.Id, request.Notes);
            AddSeasons(perfume.Id, request.Seasons);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(perfume.Id, cancellationToken);
        }, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var perfume = await _dbContext.Perfumes.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (perfume is null)
        {
            throw new NotFoundException($"Perfume with id {id} was not found.");
        }

        perfume.IsActive = false;
        perfume.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Perfume> QueryActivePerfumesBase()
    {
        return _dbContext.Perfumes
            .AsNoTracking()
            .Where(x => x.IsActive && x.Brand.IsActive);
    }

    private IQueryable<Perfume> QueryActivePerfumes()
    {
        return QueryActivePerfumesBase()
            .Include(x => x.Brand)
            .Include(x => x.FragranceType)
            .Include(x => x.PerfumeNotes)
                .ThenInclude(x => x.Note)
            .Include(x => x.PerfumeSeasons)
                .ThenInclude(x => x.Season);
    }

    private static IQueryable<Perfume> ApplyFilters(IQueryable<Perfume> query, PerfumeQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Brand.Name.ToLower().Contains(search));
        }

        if (parameters.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == parameters.BrandId.Value);
        }

        if (parameters.FragranceTypeId.HasValue)
        {
            query = query.Where(p => p.FragranceTypeId == parameters.FragranceTypeId.Value);
        }

        if (parameters.SeasonId.HasValue)
        {
            var seasonId = parameters.SeasonId.Value;
            query = query.Where(p => p.PerfumeSeasons.Any(ps => ps.SeasonId == seasonId));
        }

        var noteIds = parameters.GetNoteIds();
        foreach (var noteId in noteIds)
        {
            var capturedNoteId = noteId;
            query = query.Where(p => p.PerfumeNotes.Any(pn => pn.NoteId == capturedNoteId));
        }

        if (parameters.MinPrice.HasValue)
        {
            var minPrice = parameters.MinPrice.Value;
            query = query.Where(p => p.Price != null && p.Price >= minPrice);
        }

        if (parameters.MaxPrice.HasValue)
        {
            var maxPrice = parameters.MaxPrice.Value;
            query = query.Where(p => p.Price != null && p.Price <= maxPrice);
        }

        if (parameters.MinRating.HasValue)
        {
            var minRating = parameters.MinRating.Value;
            query = query.Where(p => p.Rating >= minRating);
        }

        return query;
    }

    private static IQueryable<Perfume> ApplySort(IQueryable<Perfume> query, string? sortBy, string? sortOrder)
    {
        var field = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var descending = string.Equals(sortOrder?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

        return field switch
        {
            "price" => descending
                ? query.OrderByDescending(p => p.Price).ThenBy(p => p.Name)
                : query.OrderBy(p => p.Price).ThenBy(p => p.Name),
            "rating" => descending
                ? query.OrderByDescending(p => p.Rating).ThenBy(p => p.Name)
                : query.OrderBy(p => p.Rating).ThenBy(p => p.Name),
            "releaseyear" => descending
                ? query.OrderByDescending(p => p.ReleaseYear).ThenBy(p => p.Name)
                : query.OrderBy(p => p.ReleaseYear).ThenBy(p => p.Name),
            _ => descending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name)
        };
    }

    private async Task ValidateRelationsAsync(
        int brandId,
        int fragranceTypeId,
        IEnumerable<PerfumeNoteInputDto> notes,
        IEnumerable<PerfumeSeasonInputDto> seasons,
        CancellationToken cancellationToken)
    {
        var brandExists = await _dbContext.Brands.AnyAsync(x => x.Id == brandId && x.IsActive, cancellationToken);
        if (!brandExists)
        {
            throw new BadRequestException($"Brand with id {brandId} was not found or is inactive.");
        }

        var fragranceTypeExists = await _dbContext.FragranceTypes.AnyAsync(x => x.Id == fragranceTypeId, cancellationToken);
        if (!fragranceTypeExists)
        {
            throw new BadRequestException($"Fragrance type with id {fragranceTypeId} was not found.");
        }

        var noteIds = notes.Select(x => x.NoteId).Distinct().ToList();
        if (noteIds.Count > 0)
        {
            var existingNoteIds = await _dbContext.Notes
                .Where(x => noteIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var missingNotes = noteIds.Except(existingNoteIds).ToList();
            if (missingNotes.Count > 0)
            {
                throw new BadRequestException($"Notes not found: {string.Join(", ", missingNotes)}.");
            }
        }

        var seasonIds = seasons.Select(x => x.SeasonId).Distinct().ToList();
        if (seasonIds.Count > 0)
        {
            var existingSeasonIds = await _dbContext.Seasons
                .Where(x => seasonIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var missingSeasons = seasonIds.Except(existingSeasonIds).ToList();
            if (missingSeasons.Count > 0)
            {
                throw new BadRequestException($"Seasons not found: {string.Join(", ", missingSeasons)}.");
            }
        }

        foreach (var note in notes)
        {
            if (!NoteTypes.IsValid(note.NoteType))
            {
                throw new BadRequestException($"Invalid note type '{note.NoteType}'.");
            }
        }
    }

    private void AddNotes(int perfumeId, IEnumerable<PerfumeNoteInputDto> notes)
    {
        foreach (var note in notes)
        {
            _dbContext.PerfumeNotes.Add(new PerfumeNote
            {
                PerfumeId = perfumeId,
                NoteId = note.NoteId,
                NoteType = NormalizeNoteType(note.NoteType)
            });
        }
    }

    private void AddSeasons(int perfumeId, IEnumerable<PerfumeSeasonInputDto> seasons)
    {
        foreach (var season in seasons)
        {
            _dbContext.PerfumeSeasons.Add(new PerfumeSeason
            {
                PerfumeId = perfumeId,
                SeasonId = season.SeasonId,
                Suitability = season.Suitability
            });
        }
    }

    private static string NormalizeNoteType(string noteType)
    {
        if (noteType.Equals(NoteTypes.Top, StringComparison.OrdinalIgnoreCase))
        {
            return NoteTypes.Top;
        }

        if (noteType.Equals(NoteTypes.Middle, StringComparison.OrdinalIgnoreCase))
        {
            return NoteTypes.Middle;
        }

        return NoteTypes.Base;
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return await action();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await action();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
