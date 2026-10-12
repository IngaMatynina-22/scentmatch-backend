using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class JournalService : IJournalService
{
    private static readonly JsonSerializerOptions TagsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ScentMatchDbContext _dbContext;

    public JournalService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<JournalEntryDto>> GetEntriesAsync(
        int userId,
        JournalQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var entries = _dbContext.ScentJournalEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (query.PerfumeId.HasValue)
        {
            entries = entries.Where(x => x.PerfumeId == query.PerfumeId.Value);
        }

        if (query.Rating.HasValue)
        {
            entries = entries.Where(x => x.Rating == query.Rating.Value);
        }

        if (query.From.HasValue)
        {
            entries = entries.Where(x => x.TestedOn >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            entries = entries.Where(x => x.TestedOn <= query.To.Value);
        }

        var totalItems = await entries.CountAsync(cancellationToken);

        var pageRows = await entries
            .OrderByDescending(x => x.TestedOn)
            .ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var perfumeIds = pageRows.Select(x => x.PerfumeId).Distinct().ToList();
        var perfumeMap = await LoadPerfumeMapAsync(perfumeIds, cancellationToken);

        var items = pageRows
            .Select(x => ToDto(x, perfumeMap.GetValueOrDefault(x.PerfumeId)))
            .ToList();

        return PagedResult<JournalEntryDto>.Create(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<JournalEntryDto> GetByIdAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetOwnedEntryAsync(userId, id, track: false, cancellationToken);
        var perfumeMap = await LoadPerfumeMapAsync([entry.PerfumeId], cancellationToken);
        return ToDto(entry, perfumeMap.GetValueOrDefault(entry.PerfumeId));
    }

    public async Task<JournalEntryDto> CreateAsync(
        int userId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfumeExists = await PerfumeQueryHelper.IsActivePerfumeAsync(
            _dbContext, request.PerfumeId, cancellationToken);
        if (!perfumeExists)
        {
            throw new NotFoundException($"Perfume with id {request.PerfumeId} was not found.");
        }

        var now = DateTime.UtcNow;
        var entry = new ScentJournalEntry
        {
            UserId = userId,
            PerfumeId = request.PerfumeId,
            TestedOn = request.TestedOn,
            Rating = request.Rating,
            Notes = NormalizeOptional(request.Notes),
            WearMinutes = request.WearMinutes,
            OpeningImpression = NormalizeOptional(request.OpeningImpression),
            HeartImpression = NormalizeOptional(request.HeartImpression),
            BaseImpression = NormalizeOptional(request.BaseImpression),
            TagsJson = SerializeTags(request.Tags),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.ScentJournalEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var perfumeMap = await LoadPerfumeMapAsync([entry.PerfumeId], cancellationToken);
        return ToDto(entry, perfumeMap.GetValueOrDefault(entry.PerfumeId));
    }

    public async Task<JournalEntryDto> UpdateAsync(
        int userId,
        int id,
        UpdateJournalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetOwnedEntryAsync(userId, id, track: true, cancellationToken);

        entry.TestedOn = request.TestedOn;
        entry.Rating = request.Rating;
        entry.Notes = NormalizeOptional(request.Notes);
        entry.WearMinutes = request.WearMinutes;
        entry.OpeningImpression = NormalizeOptional(request.OpeningImpression);
        entry.HeartImpression = NormalizeOptional(request.HeartImpression);
        entry.BaseImpression = NormalizeOptional(request.BaseImpression);
        entry.TagsJson = SerializeTags(request.Tags);
        entry.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var perfumeMap = await LoadPerfumeMapAsync([entry.PerfumeId], cancellationToken);
        return ToDto(entry, perfumeMap.GetValueOrDefault(entry.PerfumeId));
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var entry = await GetOwnedEntryAsync(userId, id, track: true, cancellationToken);
        _dbContext.ScentJournalEntries.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ScentJournalEntry> GetOwnedEntryAsync(
        int userId,
        int id,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track
            ? _dbContext.ScentJournalEntries.AsQueryable()
            : _dbContext.ScentJournalEntries.AsNoTracking();

        var entry = await query.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId,
            cancellationToken);

        if (entry is null)
        {
            throw new NotFoundException($"Journal entry with id {id} was not found.");
        }

        return entry;
    }

    private async Task<Dictionary<int, Domain.Entities.Perfume>> LoadPerfumeMapAsync(
        IReadOnlyList<int> perfumeIds,
        CancellationToken cancellationToken)
    {
        if (perfumeIds.Count == 0)
        {
            return new Dictionary<int, Domain.Entities.Perfume>();
        }

        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return perfumes.ToDictionary(x => x.Id);
    }

    private static JournalEntryDto ToDto(ScentJournalEntry entry, Domain.Entities.Perfume? perfume)
    {
        return new JournalEntryDto
        {
            Id = entry.Id,
            PerfumeId = entry.PerfumeId,
            Perfume = perfume is null ? null : CatalogMappings.ToListItemDto(perfume),
            TestedOn = entry.TestedOn,
            Rating = entry.Rating,
            Notes = entry.Notes,
            WearMinutes = entry.WearMinutes,
            OpeningImpression = entry.OpeningImpression,
            HeartImpression = entry.HeartImpression,
            BaseImpression = entry.BaseImpression,
            Tags = DeserializeTags(entry.TagsJson),
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static string? SerializeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return null;
        }

        var cleaned = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned, TagsJsonOptions);
    }

    private static IReadOnlyList<string> DeserializeTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(tagsJson, TagsJsonOptions)
                   ?? (IReadOnlyList<string>)Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
