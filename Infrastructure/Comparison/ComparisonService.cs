using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Comparison.DTOs;
using ScentMatch.Application.Comparison.Interfaces;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.Library;

namespace ScentMatch.Infrastructure.Comparison;

public class ComparisonService : IComparisonService
{
    private readonly ScentMatchDbContext _dbContext;

    public ComparisonService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ComparisonResponse> CompareAsync(
        ComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfumeIds = request.PerfumeIds;

        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (perfumes.Count != perfumeIds.Count)
        {
            // Same response for missing and inactive — do not leak catalog internals.
            throw new NotFoundException("One or more perfumes were not found.");
        }

        var perfumeMap = perfumes.ToDictionary(x => x.Id);
        var items = perfumeIds
            .Select(id => CatalogMappings.ToDetailsDto(perfumeMap[id]))
            .ToList();

        return new ComparisonResponse
        {
            Items = items,
            Analysis = BuildAnalysis(items)
        };
    }

    private static ComparisonAnalysisDto BuildAnalysis(IReadOnlyList<PerfumeDetailsDto> items)
    {
        var noteGroups = items
            .SelectMany(p => p.Notes.Select(n => new { n.NoteId, n.Name, PerfumeId = p.Id }))
            .GroupBy(x => x.NoteId)
            .Select(g => new
            {
                Note = new NamedEntityDto { Id = g.Key, Name = g.First().Name },
                Count = g.Select(x => x.PerfumeId).Distinct().Count()
            })
            .ToList();

        var seasonGroups = items
            .SelectMany(p => p.Seasons.Select(s => new { s.SeasonId, s.Name, PerfumeId = p.Id }))
            .GroupBy(x => x.SeasonId)
            .Select(g => new
            {
                Season = new NamedEntityDto { Id = g.Key, Name = g.First().Name },
                Count = g.Select(x => x.PerfumeId).Distinct().Count()
            })
            .ToList();

        var brandValues = items.Select(x => x.Brand.Name).ToList();
        var typeValues = items.Select(x => x.FragranceType.Name).ToList();
        var concentrationValues = items.Select(x => Normalize(x.Concentration)).ToList();
        var genderValues = items.Select(x => Normalize(x.Gender)).ToList();
        var ratingValues = items.Select(x => (decimal?)x.Rating).ToList();
        var priceValues = items.Select(x => x.Price).ToList();
        var yearValues = items.Select(x => x.ReleaseYear.HasValue ? (decimal?)x.ReleaseYear.Value : null).ToList();

        return new ComparisonAnalysisDto
        {
            CommonNotes = noteGroups
                .Where(x => x.Count == items.Count)
                .Select(x => x.Note)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            DifferingNotes = noteGroups
                .Where(x => x.Count < items.Count)
                .Select(x => x.Note)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CommonSeasons = seasonGroups
                .Where(x => x.Count == items.Count)
                .Select(x => x.Season)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            DifferingSeasons = seasonGroups
                .Where(x => x.Count < items.Count)
                .Select(x => x.Season)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Brand = ToScalar(brandValues),
            FragranceType = ToScalar(typeValues),
            Concentration = ToScalar(concentrationValues),
            Gender = ToScalar(genderValues),
            Rating = ToNumeric(ratingValues),
            Price = ToNumeric(priceValues),
            ReleaseYear = ToNumeric(yearValues),
            Characteristics =
            [
                Characteristic("Sweetness", items.Select(x => x.Characteristics.Sweetness).ToList()),
                Characteristic("Freshness", items.Select(x => x.Characteristics.Freshness).ToList()),
                Characteristic("Woody", items.Select(x => x.Characteristics.Woody).ToList()),
                Characteristic("Spicy", items.Select(x => x.Characteristics.Spicy).ToList()),
                Characteristic("Citrus", items.Select(x => x.Characteristics.Citrus).ToList()),
                Characteristic("Floral", items.Select(x => x.Characteristics.Floral).ToList()),
                Characteristic("Fruity", items.Select(x => x.Characteristics.Fruity).ToList()),
                Characteristic("Powdery", items.Select(x => x.Characteristics.Powdery).ToList()),
                Characteristic("Smoky", items.Select(x => x.Characteristics.Smoky).ToList()),
                Characteristic("Oriental", items.Select(x => x.Characteristics.Oriental).ToList())
            ]
        };
    }

    private static ComparisonScalarFieldDto ToScalar(IReadOnlyList<string?> values)
    {
        var normalized = values
            .Select(v => string.IsNullOrWhiteSpace(v) ? null : v.Trim())
            .ToList();

        var distinct = normalized
            .Where(v => v is not null)
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allPresent = normalized.All(v => v is not null);

        return new ComparisonScalarFieldDto
        {
            IsCommon = allPresent && distinct.Count <= 1,
            Values = normalized
        };
    }

    private static ComparisonNumericFieldDto ToNumeric(IReadOnlyList<decimal?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList();
        var allPresent = values.All(v => v.HasValue);

        return new ComparisonNumericFieldDto
        {
            IsCommon = allPresent && present.Count <= 1,
            Values = values.ToList()
        };
    }

    private static ComparisonCharacteristicFieldDto Characteristic(string name, IReadOnlyList<int?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList();
        var allPresent = values.All(v => v.HasValue);

        return new ComparisonCharacteristicFieldDto
        {
            Name = name,
            IsCommon = allPresent && present.Count <= 1,
            Values = values.ToList()
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
