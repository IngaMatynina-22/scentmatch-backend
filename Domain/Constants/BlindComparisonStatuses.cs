namespace ScentMatch.Domain.Constants;

public static class BlindComparisonStatuses
{
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Expired = "Expired";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Active, Completed, Expired
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
