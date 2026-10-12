namespace ScentMatch.Application.Common.Scoring;

/// <summary>
/// Shared deterministic scoring helpers.
/// Missing values are skipped (not treated as zero / dissimilar).
/// </summary>
public static class PerfumeScoring
{
    public const decimal NoteTopWeight = 1.0m;
    public const decimal NoteMiddleWeight = 1.2m;
    public const decimal NoteBaseWeight = 1.4m;

    public static decimal Clamp01(decimal value) => Math.Clamp(value, 0m, 1m);

    public static decimal CharacteristicSimilarity(int a, int b) =>
        Clamp01(1m - Math.Abs(a - b) / 100m);

    public static decimal PriceSimilarity(decimal price, decimal? minPrice, decimal? maxPrice)
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

    public static decimal NoteTypeWeight(string noteType) =>
        noteType.Equals("Base", StringComparison.OrdinalIgnoreCase) ? NoteBaseWeight
        : noteType.Equals("Middle", StringComparison.OrdinalIgnoreCase) ? NoteMiddleWeight
        : NoteTopWeight;

    /// <summary>
    /// Weighted Jaccard-like similarity over notes. Missing notes on either side do not invent mismatch
    /// beyond the overlap ratio of available note sets.
    /// </summary>
    public static (decimal Similarity, decimal SharedWeight, decimal UnionWeight) NoteSimilarity(
        IReadOnlyList<ProfileNote> a,
        IReadOnlyList<ProfileNote> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return (0m, 0m, 0m);
        }

        var aWeights = a
            .GroupBy(x => x.NoteId)
            .ToDictionary(g => g.Key, g => g.Max(x => NoteTypeWeight(x.NoteType)));
        var bWeights = b
            .GroupBy(x => x.NoteId)
            .ToDictionary(g => g.Key, g => g.Max(x => NoteTypeWeight(x.NoteType)));

        decimal shared = 0m;
        decimal union = 0m;
        foreach (var id in aWeights.Keys.Union(bWeights.Keys))
        {
            aWeights.TryGetValue(id, out var aw);
            bWeights.TryGetValue(id, out var bw);
            var max = Math.Max(aw, bw);
            union += max;
            if (aw > 0 && bw > 0)
            {
                shared += Math.Min(aw, bw);
            }
        }

        if (union <= 0m)
        {
            return (0m, 0m, 0m);
        }

        return (Clamp01(shared / union), shared, union);
    }

    public static decimal SeasonSimilarity(IReadOnlyList<ProfileSeason> seasons, int seasonId)
    {
        var match = seasons.FirstOrDefault(x => x.SeasonId == seasonId);
        if (match is null || !match.Suitability.HasValue)
        {
            return 0m;
        }

        return Clamp01(match.Suitability.Value / 100m);
    }

    public static IEnumerable<(string Key, int? Value)> CharacteristicPairs(PerfumeProfile p)
    {
        yield return ("Sweetness", p.Sweetness);
        yield return ("Freshness", p.Freshness);
        yield return ("Woody", p.Woody);
        yield return ("Spicy", p.Spicy);
        yield return ("Citrus", p.Citrus);
        yield return ("Floral", p.Floral);
        yield return ("Fruity", p.Fruity);
        yield return ("Powdery", p.Powdery);
        yield return ("Smoky", p.Smoky);
        yield return ("Oriental", p.Oriental);
    }

    public static decimal? GetCharacteristic(PerfumeProfile p, string key) => key switch
    {
        "Sweetness" => p.Sweetness,
        "Freshness" => p.Freshness,
        "Woody" => p.Woody,
        "Spicy" => p.Spicy,
        "Citrus" => p.Citrus,
        "Floral" => p.Floral,
        "Fruity" => p.Fruity,
        "Powdery" => p.Powdery,
        "Smoky" => p.Smoky,
        "Oriental" => p.Oriental,
        "Intensity" => p.Intensity,
        _ => null
    };

    public static ScoreBreakdown Normalize(
        IReadOnlyList<ScoreComponent> components,
        IReadOnlyList<string> positives,
        IReadOnlyList<string> mismatches,
        IReadOnlyList<string> gaps)
    {
        var active = components.Where(c => !c.SkippedDueToMissingData).ToList();
        var totalWeight = active.Sum(c => c.Weight);
        var score = totalWeight <= 0m
            ? 0m
            : Math.Round(100m * active.Sum(c => c.Weight * c.Similarity) / totalWeight, 2, MidpointRounding.AwayFromZero);

        return new ScoreBreakdown
        {
            TotalScore = Math.Clamp(score, 0m, 100m),
            Components = components.ToList(),
            PositiveReasons = positives.ToList(),
            MismatchReasons = mismatches.ToList(),
            DataGaps = gaps.ToList(),
            IncompleteData = gaps.Count > 0
        };
    }
}
