using ScentMatch.Application.Common.Scoring;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Infrastructure.Matching;

/// <summary>
/// Scores a perfume profile against matching criteria using shared PerfumeScoring helpers.
/// Missing perfume data skips the criterion (does not invent dissimilarity).
/// </summary>
public static class MatchingCriteriaScorer
{
    public static ScoreBreakdown Score(PerfumeProfile perfume, MatchingRequest request)
    {
        var components = new List<ScoreComponent>();
        var positives = new List<string>();
        var mismatches = new List<string>();
        var gaps = new List<string>();

        var requestedCharacteristics = GetRequestedCharacteristics(request);
        if (requestedCharacteristics.Count > 0)
        {
            var perWeight = MatchingWeights.Characteristics / requestedCharacteristics.Count;
            var sims = new List<decimal>();

            foreach (var (key, requested) in requestedCharacteristics)
            {
                var value = PerfumeScoring.GetCharacteristic(perfume, key);
                if (!value.HasValue)
                {
                    components.Add(Skip($"Characteristic:{key}", perWeight));
                    gaps.Add($"{key} is missing on perfume");
                    continue;
                }

                var sim = PerfumeScoring.CharacteristicSimilarity((int)value.Value, requested);
                sims.Add(sim);
                components.Add(Component($"Characteristic:{key}", perWeight, sim));

                if (sim < 0.4m)
                {
                    mismatches.Add($"{key} does not match the requested level");
                }
            }

            if (sims.Count > 0)
            {
                var avg = sims.Average();
                if (avg >= 0.75m)
                {
                    positives.Add("Aromatic characteristics closely match the requested profile");
                }
                else if (avg >= 0.45m)
                {
                    positives.Add("Aromatic characteristics partially match the requested profile");
                }
            }
        }

        if (request.SeasonId.HasValue)
        {
            var match = perfume.Seasons.FirstOrDefault(x => x.SeasonId == request.SeasonId.Value);
            if (match is null || !match.Suitability.HasValue)
            {
                components.Add(Skip("Season", MatchingWeights.Season));
                gaps.Add("Season suitability is missing");
            }
            else
            {
                var sim = PerfumeScoring.SeasonSimilarity(perfume.Seasons, request.SeasonId.Value);
                components.Add(Component("Season", MatchingWeights.Season, sim));
                if (sim >= 0.7m)
                {
                    positives.Add("Fits the selected season well");
                }
                else if (sim > 0m)
                {
                    positives.Add("Partially suitable for the selected season");
                }
                else
                {
                    mismatches.Add("Poor fit for the selected season");
                }
            }
        }

        if (request.FragranceTypeId.HasValue)
        {
            var sim = perfume.FragranceTypeId == request.FragranceTypeId.Value ? 1m : 0m;
            components.Add(Component("FragranceType", MatchingWeights.FragranceType, sim));
            if (sim == 1m)
            {
                positives.Add("Matches the selected fragrance type");
            }
            else
            {
                mismatches.Add("Fragrance type does not match");
            }
        }

        if (request.SelectedNoteIds is { Count: > 0 })
        {
            var selected = request.SelectedNoteIds.ToHashSet();
            if (perfume.Notes.Count == 0)
            {
                components.Add(Skip("Notes", MatchingWeights.Notes));
                gaps.Add("Perfume has no notes");
            }
            else
            {
                var matched = perfume.Notes.Select(n => n.NoteId).Distinct().Count(id => selected.Contains(id));
                var sim = (decimal)matched / selected.Count;
                components.Add(Component("Notes", MatchingWeights.Notes, sim));
                if (matched > 0)
                {
                    positives.Add($"Matches {matched} of {selected.Count} selected notes");
                }
                else
                {
                    mismatches.Add("None of the selected notes are present");
                }
            }
        }

        if (request.ExcludedNoteIds is { Count: > 0 })
        {
            var excluded = request.ExcludedNoteIds.ToHashSet();
            var hits = perfume.Notes.Select(n => n.NoteId).Distinct().Count(id => excluded.Contains(id));
            if (hits > 0)
            {
                mismatches.Add($"Contains {hits} excluded note(s)");
            }
        }

        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            if (!perfume.Price.HasValue)
            {
                components.Add(Skip("Price", MatchingWeights.Price));
                gaps.Add("Price is missing");
            }
            else
            {
                var sim = PerfumeScoring.PriceSimilarity(
                    perfume.Price.Value,
                    request.MinPrice,
                    request.MaxPrice);
                components.Add(Component("Price", MatchingWeights.Price, sim));
                if (sim >= 1m)
                {
                    positives.Add("Price is within the requested range");
                }
                else if (sim >= 0.5m)
                {
                    positives.Add("Price is close to the requested range");
                }
                else
                {
                    mismatches.Add("Price is outside the requested range");
                }
            }
        }

        if (request.Intensity.HasValue)
        {
            if (!perfume.Intensity.HasValue)
            {
                components.Add(Skip("Intensity", MatchingWeights.Intensity));
                gaps.Add("Intensity is missing");
            }
            else
            {
                var sim = PerfumeScoring.CharacteristicSimilarity(
                    perfume.Intensity.Value,
                    request.Intensity.Value);
                components.Add(Component("Intensity", MatchingWeights.Intensity, sim));
                if (sim >= 0.75m)
                {
                    positives.Add("Intensity matches the request");
                }
                else if (sim < 0.4m)
                {
                    mismatches.Add("Intensity does not match the request");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Concentration))
        {
            if (string.IsNullOrWhiteSpace(perfume.Concentration))
            {
                components.Add(Skip("Concentration", MatchingWeights.Concentration));
                gaps.Add("Concentration is missing");
            }
            else
            {
                var sim = ConcentrationsMatch(perfume.Concentration, request.Concentration) ? 1m : 0m;
                components.Add(Component("Concentration", MatchingWeights.Concentration, sim));
                if (sim == 1m)
                {
                    positives.Add("Concentration matches");
                }
                else
                {
                    mismatches.Add("Concentration does not match");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (string.IsNullOrWhiteSpace(perfume.Gender))
            {
                components.Add(Skip("Gender", MatchingWeights.Gender));
                gaps.Add("Gender is missing");
            }
            else
            {
                var sim = GendersMatch(perfume.Gender, request.Gender) ? 1m : 0m;
                components.Add(Component("Gender", MatchingWeights.Gender, sim));
                if (sim == 1m)
                {
                    positives.Add("Gender preference matches");
                }
                else
                {
                    mismatches.Add("Gender preference does not match");
                }
            }
        }

        return PerfumeScoring.Normalize(components, positives, mismatches, gaps);
    }

    public static MatchingResultDto ToMatchingResult(PerfumeProfile perfume, ScoreBreakdown breakdown) => new()
    {
        PerfumeId = perfume.Id,
        Name = perfume.Name,
        BrandName = perfume.BrandName,
        Price = perfume.Price,
        Score = breakdown.TotalScore,
        Reasons = breakdown.PositiveReasons,
        Components = breakdown.Components,
        PositiveReasons = breakdown.PositiveReasons,
        MismatchReasons = breakdown.MismatchReasons,
        DataGaps = breakdown.DataGaps
    };

    private static bool ConcentrationsMatch(string perfume, string requested)
    {
        static string Norm(string s) => s.Trim().Replace(".", string.Empty).Replace(" ", string.Empty)
            .ToUpperInvariant();

        var a = Norm(perfume);
        var b = Norm(requested);
        if (a == b)
        {
            return true;
        }

        // Common aliases
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EDT"] = "EAUDETOILETTE",
            ["EAUDETOILETTE"] = "EAUDETOILETTE",
            ["EDP"] = "EAUDEPARFUM",
            ["EAUDEPARFUM"] = "EAUDEPARFUM",
            ["PARFUM"] = "PARFUM",
            ["EXTRAIT"] = "PARFUM",
            ["COLOGNE"] = "EAUDECOLOGNE",
            ["EDC"] = "EAUDECOLOGNE",
            ["EAUDECOLOGNE"] = "EAUDECOLOGNE"
        };

        aliases.TryGetValue(a, out var na);
        aliases.TryGetValue(b, out var nb);
        return !string.IsNullOrEmpty(na) && na == nb;
    }

    private static bool GendersMatch(string perfumeGender, string requestedGender)
    {
        var p = perfumeGender.Trim();
        var r = requestedGender.Trim();

        if (p.Equals(r, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (p.Equals("Unisex", StringComparison.OrdinalIgnoreCase)
            || r.Equals("Unisex", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<(string Key, int Value)> GetRequestedCharacteristics(MatchingRequest request)
    {
        var list = new List<(string, int)>();
        void Add(string key, int? value)
        {
            if (value.HasValue)
            {
                list.Add((key, value.Value));
            }
        }

        Add("Sweetness", request.Sweetness);
        Add("Freshness", request.Freshness);
        Add("Woody", request.Woody);
        Add("Spicy", request.Spicy);
        Add("Citrus", request.Citrus);
        Add("Floral", request.Floral);
        Add("Fruity", request.Fruity);
        Add("Powdery", request.Powdery);
        Add("Smoky", request.Smoky);
        Add("Oriental", request.Oriental);
        return list;
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
