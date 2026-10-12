namespace ScentMatch.Domain.Constants;

public static class WishlistStatuses
{
    public const string Wanted = "Wanted";
    public const string Watching = "Watching";
    public const string Purchased = "Purchased";
    public const string Archived = "Archived";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Wanted, Watching, Purchased, Archived
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);

    public static string Normalize(string value) =>
        All.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));

    public static string AllowedList => string.Join(", ", All.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
}
