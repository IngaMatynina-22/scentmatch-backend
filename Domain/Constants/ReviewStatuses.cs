namespace ScentMatch.Domain.Constants;

public static class ReviewStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Approved,
        Rejected
    };

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status);

    public static string Normalize(string status) =>
        All.First(x => x.Equals(status, StringComparison.OrdinalIgnoreCase));
}
