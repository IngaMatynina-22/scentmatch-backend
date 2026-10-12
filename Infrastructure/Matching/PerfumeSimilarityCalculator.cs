using ScentMatch.Application.Common.Scoring;

namespace ScentMatch.Infrastructure.Matching;

/// <summary>
/// Deterministic perfume-to-perfume similarity. Missing values are skipped, never treated as dissimilar.
/// </summary>
public static class PerfumeSimilarityCalculator
{
    public const decimal NotesWeight = 35m;
    public const decimal FragranceTypeWeight = 15m;
    public const decimal SeasonsWeight = 15m;
    public const decimal CharacteristicsWeight = 25m;
    public const decimal IntensityWeight = 5m;
    public const decimal PriceWeight = 5m;

    /// <summary>Relative price band around the source perfume (± this fraction).</summary>
    public const decimal PriceBandFraction = 0.25m;

    public static ScoreBreakdown Calculate(PerfumeProfile source, PerfumeProfile candidate)
    {
        var components = new List<ScoreComponent>();
        var positives = new List<string>();
        var mismatches = new List<string>();
        var gaps = new List<string>();

        AddNotes(source, candidate, components, positives, mismatches, gaps);
        AddFragranceType(source, candidate, components, positives, mismatches);
        AddSeasons(source, candidate, components, positives, mismatches, gaps);
        AddCharacteristics(source, candidate, components, positives, mismatches, gaps);
        AddIntensity(source, candidate, components, positives, mismatches, gaps);
        AddPrice(source, candidate, components, positives, mismatches, gaps);

        return PerfumeScoring.Normalize(components, positives, mismatches, gaps);
    }

    private static void AddNotes(
        PerfumeProfile source,
        PerfumeProfile candidate,
        List<ScoreComponent> components,
        List<string> positives,
        List<string> mismatches,
        List<string> gaps)
    {
        if (source.Notes.Count == 0 || candidate.Notes.Count == 0)
        {
            components.Add(Skip("Notes", NotesWeight));
            gaps.Add(source.Notes.Count == 0
                ? "Source perfume has no notes"
                : "Candidate perfume has no notes");
            return;
        }

        var (similarity, sharedWeight, _) = PerfumeScoring.NoteSimilarity(source.Notes, candidate.Notes);
        components.Add(Component("Notes", NotesWeight, similarity));

        if (similarity >= 0.6m)
        {
            positives.Add("Shared notes with similar pyramid weights");
        }
        else if (similarity > 0m && sharedWeight > 0m)
        {
            positives.Add("Partial note overlap");
        }
        else
        {
            mismatches.Add("Little or no note overlap");
        }
    }

    private static void AddFragranceType(
        PerfumeProfile source,
        PerfumeProfile candidate,
        List<ScoreComponent> components,
        List<string> positives,
        List<string> mismatches)
    {
        var similarity = source.FragranceTypeId == candidate.FragranceTypeId ? 1m : 0m;
        components.Add(Component("FragranceType", FragranceTypeWeight, similarity));

        if (similarity == 1m)
        {
            positives.Add($"Same fragrance type ({source.FragranceTypeName})");
        }
        else
        {
            mismatches.Add(
                $"Different fragrance type ({source.FragranceTypeName} vs {candidate.FragranceTypeName})");
        }
    }

    private static void AddSeasons(
        PerfumeProfile source,
        PerfumeProfile candidate,
        List<ScoreComponent> components,
        List<string> positives,
        List<string> mismatches,
        List<string> gaps)
    {
        if (source.Seasons.Count == 0 || candidate.Seasons.Count == 0)
        {
            components.Add(Skip("Seasons", SeasonsWeight));
            gaps.Add(source.Seasons.Count == 0
                ? "Source perfume has no season data"
                : "Candidate perfume has no season data");
            return;
        }

        var sourceById = source.Seasons.ToDictionary(x => x.SeasonId);
        var candidateById = candidate.Seasons.ToDictionary(x => x.SeasonId);
        var sharedIds = sourceById.Keys.Intersect(candidateById.Keys).ToList();

        if (sharedIds.Count == 0)
        {
            // No shared season rows — do not invent dissimilarity from missing overlap alone.
            components.Add(Skip("Seasons", SeasonsWeight));
            gaps.Add("No overlapping season entries to compare");
            return;
        }

        var similarities = new List<decimal>();
        foreach (var id in sharedIds)
        {
            var a = sourceById[id].Suitability;
            var b = candidateById[id].Suitability;
            if (!a.HasValue || !b.HasValue)
            {
                continue;
            }

            similarities.Add(PerfumeScoring.CharacteristicSimilarity(a.Value, b.Value));
        }

        if (similarities.Count == 0)
        {
            components.Add(Skip("Seasons", SeasonsWeight));
            gaps.Add("Shared seasons lack suitability values");
            return;
        }

        var similarity = similarities.Average();
        components.Add(Component("Seasons", SeasonsWeight, similarity));

        if (similarity >= 0.7m)
        {
            positives.Add("Similar seasonal suitability");
        }
        else if (similarity < 0.4m)
        {
            mismatches.Add("Seasonal suitability differs");
        }
    }

    private static void AddCharacteristics(
        PerfumeProfile source,
        PerfumeProfile candidate,
        List<ScoreComponent> components,
        List<string> positives,
        List<string> mismatches,
        List<string> gaps)
    {
        var pairs = new List<(string Key, int A, int B)>();
        foreach (var (key, _) in PerfumeScoring.CharacteristicPairs(source))
        {
            var a = PerfumeScoring.GetCharacteristic(source, key);
            var b = PerfumeScoring.GetCharacteristic(candidate, key);
            if (a.HasValue && b.HasValue)
            {
                pairs.Add((key, (int)a.Value, (int)b.Value));
            }
        }

        if (pairs.Count == 0)
        {
            components.Add(Skip("Characteristics", CharacteristicsWeight));
            gaps.Add("No overlapping characteristic values to compare");
            return;
        }

        var perWeight = CharacteristicsWeight / pairs.Count;
        var sims = new List<decimal>();
        foreach (var (key, a, b) in pairs)
        {
            var sim = PerfumeScoring.CharacteristicSimilarity(a, b);
            sims.Add(sim);
            components.Add(Component($"Characteristic:{key}", perWeight, sim));

            if (sim >= 0.85m)
            {
                positives.Add($"{key} is very similar");
            }
            else if (sim < 0.4m)
            {
                mismatches.Add($"{key} differs substantially");
            }
        }

        // Keep a rollup for readability when many characteristics exist.
        _ = sims;
    }

    private static void AddIntensity(
        PerfumeProfile source,
        PerfumeProfile candidate,
        List<ScoreComponent> components,
        List<string> positives,
        List<string> mismatches,
        List<string> gaps)
    {
        if (!source.Intensity.HasValue || !candidate.Intensity.HasValue)
        {
            components.Add(Skip("Intensity", IntensityWeight));
            if (!source.Intensity.HasValue || !candidate.Intensity.HasValue)
            {
                gaps.Add("Intensity missing on one or both perfumes");
            }

            return;
        }

        var similarity = PerfumeScoring.CharacteristicSimilarity(
            source.Intensity.Value,
            candidate.Intensity.Value);
        components.Add(Component("Intensity", IntensityWeight, similarity));

        if (similarity >= 0.8m)
        {
            positives.Add("Similar intensity");
        }
        else if (similarity < 0.4m)
        {
            mismatches.Add("Intensity differs");
        }
    }

    private static void AddPrice(
        PerfumeProfile source,
        PerfumeProfile candidate,
        List<ScoreComponent> components,
        List<string> positives,
        List<string> mismatches,
        List<string> gaps)
    {
        if (!source.Price.HasValue || !candidate.Price.HasValue)
        {
            components.Add(Skip("Price", PriceWeight));
            gaps.Add("Price missing on one or both perfumes");
            return;
        }

        var center = source.Price.Value;
        var span = Math.Max(center * PriceBandFraction, 1m);
        var min = center - span;
        var max = center + span;
        var similarity = PerfumeScoring.PriceSimilarity(candidate.Price.Value, min, max);
        components.Add(Component("Price", PriceWeight, similarity));

        if (similarity >= 1m)
        {
            positives.Add("Price is within a similar band");
        }
        else if (similarity < 0.5m)
        {
            mismatches.Add("Price is outside a similar band");
        }
    }

    private static ScoreComponent Component(string key, decimal weight, decimal similarity) => new()
    {
        Key = key,
        Weight = weight,
        Similarity = PerfumeScoring.Clamp01(similarity),
        Contribution = weight * PerfumeScoring.Clamp01(similarity),
        SkippedDueToMissingData = false
    };

    private static ScoreComponent Skip(string key, decimal weight) => new()
    {
        Key = key,
        Weight = weight,
        Similarity = 0m,
        Contribution = 0m,
        SkippedDueToMissingData = true
    };
}
