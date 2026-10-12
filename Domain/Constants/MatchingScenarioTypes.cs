namespace ScentMatch.Domain.Constants;

public static class MatchingScenarioTypes
{
    public const string Personal = "Personal";
    public const string Gift = "Gift";
    public const string Text = "Text";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Personal, Gift, Text
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);

    public static string Normalize(string value) =>
        All.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
}
