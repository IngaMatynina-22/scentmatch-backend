namespace ScentMatch.Domain.Constants;

public static class NotificationTypes
{
    public const string System = "System";
    public const string Review = "Review";
    public const string Recommendation = "Recommendation";
    public const string Info = "Info";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        System,
        Review,
        Recommendation,
        Info
    };

    public static bool IsValid(string? type) =>
        !string.IsNullOrWhiteSpace(type) && All.Contains(type);

    public static string Normalize(string type) =>
        All.First(x => x.Equals(type, StringComparison.OrdinalIgnoreCase));
}
