using System.Globalization;
using System.Text.RegularExpressions;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Infrastructure.Matching;

public sealed class TextCriteriaCatalog
{
    public IReadOnlyList<CatalogName> Notes { get; init; } = Array.Empty<CatalogName>();
    public IReadOnlyList<CatalogName> Seasons { get; init; } = Array.Empty<CatalogName>();
    public IReadOnlyList<CatalogName> FragranceTypes { get; init; } = Array.Empty<CatalogName>();
}

public sealed record CatalogName(int Id, string Name);

public sealed class TextParseResult
{
    public ParsedTextCriteriaDto Criteria { get; init; } = new();
    public IReadOnlyList<string> AmbiguousCriteria { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Deterministic UA+EN free-text criteria parser. Unit-testable; no LLM.
/// </summary>
public static class TextCriteriaParser
{
    private static readonly Regex BudgetRegex = new(
        @"(?:до|under|below|max(?:imum)?|up\s+to)\s*(?<cur>€|\$|eur|usd|uah|грн)?\s*(?<amount>\d+(?:[.,]\d+)?)\s*(?<cur2>€|\$|eur|usd|uah|грн)?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BudgetBetweenRegex = new(
        @"(?:від|from)\s*(?<min>\d+(?:[.,]\d+)?)\s*(?:до|to|-|–)\s*(?<max>\d+(?:[.,]\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly (string Token, int Value)[] IntensityMap =
    [
        ("легк", 25),
        ("light", 25),
        ("soft", 25),
        ("середн", 50),
        ("medium", 50),
        ("moderate", 50),
        ("сильн", 80),
        ("strong", 80),
        ("intense", 85),
        ("потужн", 85),
        ("heavy", 85)
    ];

    private static readonly (string Token, string Value)[] ConcentrationMap =
    [
        ("extrait", "Parfum"),
        ("parfum", "Parfum"),
        ("edp", "EDP"),
        ("eau de parfum", "EDP"),
        ("edt", "EDT"),
        ("eau de toilette", "EDT"),
        ("edc", "EDC"),
        ("eau de cologne", "EDC"),
        ("cologne", "EDC")
    ];

    private static readonly (string Token, string Gender)[] GenderMap =
    [
        ("для чоловік", "Male"),
        ("для чоловіків", "Male"),
        ("чоловіч", "Male"),
        ("for men", "Male"),
        ("men's", "Male"),
        ("masculine", "Male"),
        ("для жінок", "Female"),
        ("для жінки", "Female"),
        ("жіноч", "Female"),
        ("for women", "Female"),
        ("women's", "Female"),
        ("feminine", "Female"),
        ("унісекс", "Unisex"),
        ("unisex", "Unisex")
    ];

    private static readonly Dictionary<string, string[]> SeasonAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Spring"] = ["spring", "весна", "весни", "веснян"],
        ["Summer"] = ["summer", "літо", "літа", "літн"],
        ["Autumn"] = ["autumn", "fall", "осінь", "осені", "осінн"],
        ["Winter"] = ["winter", "зима", "зими", "зимов"],
        ["All Season"] = ["all season", "all-season", "будь-який сезон", "на всі сезони"]
    };

    public static TextParseResult Parse(string text, TextCriteriaCatalog catalog)
    {
        var criteria = new ParsedTextCriteriaDto();
        var ambiguous = new List<string>();
        var normalized = text ?? string.Empty;
        var lower = normalized.ToLowerInvariant();

        ParseBudget(normalized, criteria);
        ParseIntensity(lower, criteria);
        ParseConcentration(lower, criteria);
        ParseGender(lower, criteria);
        ParseSeason(lower, catalog, criteria, ambiguous);
        ParseFragranceType(lower, catalog, criteria, ambiguous);
        ParseNotes(normalized, lower, catalog, criteria, ambiguous);

        return new TextParseResult
        {
            Criteria = criteria,
            AmbiguousCriteria = ambiguous
        };
    }

    public static MatchingRequest ToMatchingRequest(ParsedTextCriteriaDto criteria) => new()
    {
        SeasonId = criteria.SeasonId,
        FragranceTypeId = criteria.FragranceTypeId,
        MinPrice = criteria.MinPrice,
        MaxPrice = criteria.MaxPrice,
        Intensity = criteria.Intensity,
        Concentration = criteria.Concentration,
        Gender = criteria.Gender,
        SelectedNoteIds = criteria.PreferredNotes.Select(x => x.Id).ToList(),
        ExcludedNoteIds = criteria.ExcludedNotes.Select(x => x.Id).ToList()
    };

    private static void ParseBudget(string text, ParsedTextCriteriaDto criteria)
    {
        var between = BudgetBetweenRegex.Match(text);
        if (between.Success)
        {
            criteria.MinPrice = ParseDecimal(between.Groups["min"].Value);
            criteria.MaxPrice = ParseDecimal(between.Groups["max"].Value);
        }

        var match = BudgetRegex.Match(text);
        if (!match.Success)
        {
            return;
        }

        var amount = ParseDecimal(match.Groups["amount"].Value);
        if (!amount.HasValue)
        {
            return;
        }

        criteria.MaxPrice ??= amount;
        var cur = FirstNonEmpty(match.Groups["cur"].Value, match.Groups["cur2"].Value);
        if (!string.IsNullOrWhiteSpace(cur))
        {
            criteria.Currency = NormalizeCurrency(cur);
        }
    }

    private static void ParseIntensity(string lower, ParsedTextCriteriaDto criteria)
    {
        foreach (var (token, value) in IntensityMap)
        {
            if (lower.Contains(token, StringComparison.Ordinal))
            {
                criteria.Intensity = value;
                return;
            }
        }
    }

    private static void ParseConcentration(string lower, ParsedTextCriteriaDto criteria)
    {
        foreach (var (token, value) in ConcentrationMap.OrderByDescending(x => x.Token.Length))
        {
            if (lower.Contains(token, StringComparison.Ordinal))
            {
                criteria.Concentration = value;
                return;
            }
        }
    }

    private static void ParseGender(string lower, ParsedTextCriteriaDto criteria)
    {
        foreach (var (token, gender) in GenderMap.OrderByDescending(x => x.Token.Length))
        {
            if (lower.Contains(token, StringComparison.Ordinal))
            {
                criteria.Gender = gender;
                return;
            }
        }
    }

    private static void ParseSeason(
        string lower,
        TextCriteriaCatalog catalog,
        ParsedTextCriteriaDto criteria,
        List<string> ambiguous)
    {
        var matches = new List<CatalogName>();
        foreach (var season in catalog.Seasons)
        {
            var aliases = SeasonAliases.TryGetValue(season.Name, out var list)
                ? list.Append(season.Name.ToLowerInvariant())
                : [season.Name.ToLowerInvariant()];

            if (aliases.Any(a => lower.Contains(a, StringComparison.Ordinal)))
            {
                matches.Add(season);
            }
        }

        AssignUnique(matches, ambiguous, "season", (id, name) =>
        {
            criteria.SeasonId = id;
            criteria.SeasonName = name;
        });
    }

    private static void ParseFragranceType(
        string lower,
        TextCriteriaCatalog catalog,
        ParsedTextCriteriaDto criteria,
        List<string> ambiguous)
    {
        var matches = catalog.FragranceTypes
            .Where(ft => lower.Contains(ft.Name.ToLowerInvariant(), StringComparison.Ordinal))
            .ToList();

        // UA aliases for common types
        var uaAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Woody"] = ["дерев", "woody"],
            ["Floral"] = ["квітков", "floral"],
            ["Oriental"] = ["східн", "oriental"],
            ["Fresh"] = ["свіж", "fresh"],
            ["Citrus"] = ["цитрус", "citrus"],
            ["Fruity"] = ["фрукт", "fruity"],
            ["Gourmand"] = ["гурман", "gourmand"],
            ["Aromatic"] = ["ароматич", "aromatic"],
            ["Chypre"] = ["шипр", "chypre"],
            ["Leather"] = ["шкір", "leather"]
        };

        foreach (var ft in catalog.FragranceTypes)
        {
            if (matches.Any(m => m.Id == ft.Id))
            {
                continue;
            }

            if (uaAliases.TryGetValue(ft.Name, out var aliases)
                && aliases.Any(a => lower.Contains(a, StringComparison.Ordinal)))
            {
                matches.Add(ft);
            }
        }

        AssignUnique(matches.DistinctBy(x => x.Id).ToList(), ambiguous, "fragrance type", (id, name) =>
        {
            criteria.FragranceTypeId = id;
            criteria.FragranceTypeName = name;
        });
    }

    private static void ParseNotes(
        string original,
        string lower,
        TextCriteriaCatalog catalog,
        ParsedTextCriteriaDto criteria,
        List<string> ambiguous)
    {
        var preferred = new List<NamedIdDto>();
        var excluded = new List<NamedIdDto>();

        // Longest names first to prefer "Orange Blossom" over "Orange"
        foreach (var note in catalog.Notes.OrderByDescending(n => n.Name.Length))
        {
            var noteLower = note.Name.ToLowerInvariant();
            if (!lower.Contains(noteLower, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsExcludedContext(original, note.Name))
            {
                if (excluded.All(x => x.Id != note.Id))
                {
                    excluded.Add(new NamedIdDto { Id = note.Id, Name = note.Name });
                }
            }
            else if (preferred.All(x => x.Id != note.Id) && excluded.All(x => x.Id != note.Id))
            {
                preferred.Add(new NamedIdDto { Id = note.Id, Name = note.Name });
            }
        }

        // Detect ambiguous short tokens that match multiple notes
        var shortHits = catalog.Notes
            .Where(n => n.Name.Contains(' ', StringComparison.Ordinal) == false)
            .GroupBy(n => n.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Where(token => lower.Contains(token, StringComparison.Ordinal))
            .ToList();

        foreach (var token in shortHits)
        {
            ambiguous.Add($"Ambiguous note token '{token}'");
        }

        criteria.PreferredNotes = preferred;
        criteria.ExcludedNotes = excluded;
    }

    private static bool IsExcludedContext(string text, string noteName)
    {
        // Match "без vanilla", "without vanilla", "no vanilla", "окрім vanilla"
        var pattern = $@"(?:\bбез\b|\bwithout\b|\bno\b|\bокрім\b|\bexcept\b)\s+{Regex.Escape(noteName)}";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static void AssignUnique(
        IReadOnlyList<CatalogName> matches,
        List<string> ambiguous,
        string label,
        Action<int, string> assign)
    {
        if (matches.Count == 1)
        {
            assign(matches[0].Id, matches[0].Name);
        }
        else if (matches.Count > 1)
        {
            ambiguous.Add(
                $"Ambiguous {label}: {string.Join(", ", matches.Select(m => m.Name))}");
        }
    }

    private static decimal? ParseDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string? FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string NormalizeCurrency(string raw) => raw.Trim().ToUpperInvariant() switch
    {
        "€" or "EUR" => "EUR",
        "$" or "USD" => "USD",
        "ГРН" or "UAH" => "UAH",
        _ => raw.Trim().ToUpperInvariant()
    };
}
