using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Matching;

public class MatchingService : IMatchingService
{
    private readonly ScentMatchDbContext _dbContext;

    public MatchingService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MatchingResponse> MatchAsync(
        MatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(request, cancellationToken);

        var selectedNoteIds = request.SelectedNoteIds is { Count: > 0 }
            ? request.SelectedNoteIds.ToHashSet()
            : null;

        var requestedCharacteristics = GetRequestedCharacteristics(request);

        var candidates = await _dbContext.Perfumes
            .AsNoTracking()
            .Where(x => x.IsActive && x.Brand.IsActive)
            .Select(x => new PerfumeCandidate
            {
                Id = x.Id,
                Name = x.Name,
                BrandName = x.Brand.Name,
                Price = x.Price,
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
                NoteIds = x.PerfumeNotes.Select(n => n.NoteId).ToList(),
                SeasonSuitabilities = x.PerfumeSeasons
                    .Select(s => new SeasonSuitability(s.SeasonId, s.Suitability))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var results = candidates
            .Select(perfume => ScorePerfume(perfume, request, requestedCharacteristics, selectedNoteIds))
            .Where(x => x is not null && x.Score > 0m)
            .Select(x => x!)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PerfumeId)
            .Take(MatchingWeights.MaxResults)
            .ToList();

        return new MatchingResponse { Items = results };
    }

    private async Task ValidateForeignKeysAsync(MatchingRequest request, CancellationToken cancellationToken)
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

        if (request.SelectedNoteIds is { Count: > 0 })
        {
            var noteIds = request.SelectedNoteIds.ToList();
            var existingCount = await _dbContext.Notes
                .AsNoTracking()
                .CountAsync(x => noteIds.Contains(x.Id), cancellationToken);

            if (existingCount != noteIds.Count)
            {
                throw new BadRequestException("One or more selected notes were not found.");
            }
        }
    }

    private static MatchingResultDto? ScorePerfume(
        PerfumeCandidate perfume,
        MatchingRequest request,
        IReadOnlyList<CharacteristicCriterion> requestedCharacteristics,
        HashSet<int>? selectedNoteIds)
    {
        decimal weightedSum = 0m;
        decimal totalWeight = 0m;
        var reasons = new List<string>();

        if (requestedCharacteristics.Count > 0)
        {
            var perCharWeight = MatchingWeights.Characteristics / requestedCharacteristics.Count;
            var similarities = new List<decimal>();

            foreach (var criterion in requestedCharacteristics)
            {
                var perfumeValue = criterion.Selector(perfume);
                if (!perfumeValue.HasValue)
                {
                    continue;
                }

                var similarity = CharacteristicSimilarity(perfumeValue.Value, criterion.RequestedValue);
                similarities.Add(similarity);
                weightedSum += perCharWeight * similarity;
                totalWeight += perCharWeight;
            }

            if (similarities.Count > 0)
            {
                var avg = similarities.Average();
                if (avg >= 0.75m)
                {
                    reasons.Add("Aromatic characteristics closely match the requested profile");
                }
                else if (avg >= 0.45m)
                {
                    reasons.Add("Aromatic characteristics partially match the requested profile");
                }
            }
        }

        if (request.SeasonId.HasValue)
        {
            var suitability = perfume.SeasonSuitabilities
                .FirstOrDefault(x => x.SeasonId == request.SeasonId.Value)
                ?.Suitability;

            // Missing season link => 0 similarity (lowers score, does not exclude).
            var similarity = suitability.HasValue
                ? Clamp01(suitability.Value / 100m)
                : 0m;

            weightedSum += MatchingWeights.Season * similarity;
            totalWeight += MatchingWeights.Season;

            if (similarity >= 0.7m)
            {
                reasons.Add("Fits the selected season well");
            }
            else if (similarity > 0m)
            {
                reasons.Add("Partially suitable for the selected season");
            }
        }

        if (request.FragranceTypeId.HasValue)
        {
            var similarity = perfume.FragranceTypeId == request.FragranceTypeId.Value ? 1m : 0m;
            weightedSum += MatchingWeights.FragranceType * similarity;
            totalWeight += MatchingWeights.FragranceType;

            if (similarity == 1m)
            {
                reasons.Add("Matches the selected fragrance type");
            }
        }

        if (selectedNoteIds is { Count: > 0 })
        {
            var matched = perfume.NoteIds.Count(id => selectedNoteIds.Contains(id));
            var similarity = (decimal)matched / selectedNoteIds.Count;

            weightedSum += MatchingWeights.Notes * similarity;
            totalWeight += MatchingWeights.Notes;

            if (matched > 0)
            {
                reasons.Add($"Matches {matched} of {selectedNoteIds.Count} selected notes");
            }
        }

        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            if (perfume.Price.HasValue)
            {
                var similarity = PriceSimilarity(perfume.Price.Value, request.MinPrice, request.MaxPrice);
                weightedSum += MatchingWeights.Price * similarity;
                totalWeight += MatchingWeights.Price;

                if (similarity >= 1m)
                {
                    reasons.Add("Price is within the requested range");
                }
                else if (similarity >= 0.5m)
                {
                    reasons.Add("Price is close to the requested range");
                }
            }
            // Null perfume price: skip price criterion (do not treat as zero).
        }

        if (totalWeight <= 0m)
        {
            // Request had criteria, but this perfume had no comparable data for any of them.
            return null;
        }

        var score = Math.Round(
            100m * weightedSum / totalWeight,
            2,
            MidpointRounding.AwayFromZero);

        score = Math.Clamp(score, 0m, 100m);

        return new MatchingResultDto
        {
            PerfumeId = perfume.Id,
            Name = perfume.Name,
            BrandName = perfume.BrandName,
            Price = perfume.Price,
            Score = score,
            Reasons = reasons
        };
    }

    private static decimal CharacteristicSimilarity(int perfumeValue, int requestedValue)
    {
        return Clamp01(1m - Math.Abs(perfumeValue - requestedValue) / 100m);
    }

    private static decimal PriceSimilarity(decimal price, decimal? minPrice, decimal? maxPrice)
    {
        var inRange =
            (!minPrice.HasValue || price >= minPrice.Value)
            && (!maxPrice.HasValue || price <= maxPrice.Value);

        if (inRange)
        {
            return 1m;
        }

        decimal distance;
        if (minPrice.HasValue && price < minPrice.Value)
        {
            distance = minPrice.Value - price;
        }
        else if (maxPrice.HasValue && price > maxPrice.Value)
        {
            distance = price - maxPrice.Value;
        }
        else
        {
            return 0m;
        }

        var span = (maxPrice ?? minPrice ?? 0m) - (minPrice ?? 0m);
        if (span <= 0m)
        {
            span = minPrice ?? maxPrice ?? 100m;
        }

        if (span <= 0m)
        {
            span = 100m;
        }

        return Clamp01(1m - distance / span);
    }

    private static decimal Clamp01(decimal value) => Math.Clamp(value, 0m, 1m);

    private static IReadOnlyList<CharacteristicCriterion> GetRequestedCharacteristics(MatchingRequest request)
    {
        var list = new List<CharacteristicCriterion>();

        void Add(int? requested, Func<PerfumeCandidate, int?> selector)
        {
            if (requested.HasValue)
            {
                list.Add(new CharacteristicCriterion(requested.Value, selector));
            }
        }

        Add(request.Sweetness, p => p.Sweetness);
        Add(request.Freshness, p => p.Freshness);
        Add(request.Woody, p => p.Woody);
        Add(request.Spicy, p => p.Spicy);
        Add(request.Citrus, p => p.Citrus);
        Add(request.Floral, p => p.Floral);
        Add(request.Fruity, p => p.Fruity);
        Add(request.Powdery, p => p.Powdery);
        Add(request.Smoky, p => p.Smoky);
        Add(request.Oriental, p => p.Oriental);

        return list;
    }

    private sealed class PerfumeCandidate
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string BrandName { get; init; } = string.Empty;
        public decimal? Price { get; init; }
        public int FragranceTypeId { get; init; }
        public int? Sweetness { get; init; }
        public int? Freshness { get; init; }
        public int? Woody { get; init; }
        public int? Spicy { get; init; }
        public int? Citrus { get; init; }
        public int? Floral { get; init; }
        public int? Fruity { get; init; }
        public int? Powdery { get; init; }
        public int? Smoky { get; init; }
        public int? Oriental { get; init; }
        public List<int> NoteIds { get; init; } = [];
        public List<SeasonSuitability> SeasonSuitabilities { get; init; } = [];
    }

    private sealed record SeasonSuitability(int SeasonId, int? Suitability);

    private sealed record CharacteristicCriterion(int RequestedValue, Func<PerfumeCandidate, int?> Selector);
}
