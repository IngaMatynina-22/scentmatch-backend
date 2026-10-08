namespace ScentMatch.Infrastructure.Matching;

/// <summary>
/// Relative weights for matching criteria.
/// Only criteria present in the request participate in score normalization.
/// </summary>
internal static class MatchingWeights
{
    /// <summary>Total weight for aromatic characteristics (split across provided ones).</summary>
    public const decimal Characteristics = 30m;

    public const decimal Season = 20m;
    public const decimal FragranceType = 15m;
    public const decimal Notes = 20m;
    public const decimal Price = 15m;

    /// <summary>Maximum number of ranked results returned by the matching engine.</summary>
    public const int MaxResults = 50;
}
