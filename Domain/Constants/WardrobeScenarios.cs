namespace ScentMatch.Domain.Constants;

public static class WardrobeScenarios
{
    public const string Daily = "Daily";
    public const string Work = "Work";
    public const string Evening = "Evening";
    public const string Date = "Date";
    public const string Summer = "Summer";
    public const string Winter = "Winter";
    public const string Special = "Special";
    public const string Custom = "Custom";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Daily, Work, Evening, Date, Summer, Winter, Special, Custom
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);

    public static string Normalize(string value) =>
        All.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));

    public static string AllowedList => string.Join(", ", All.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
}
