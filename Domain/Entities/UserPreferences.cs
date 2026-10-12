namespace ScentMatch.Domain.Entities;

public class UserPreferences
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? PreferredSeasonId { get; set; }
    public int? PreferredFragranceTypeId { get; set; }

    public int? Sweetness { get; set; }
    public int? Freshness { get; set; }
    public int? Woody { get; set; }
    public int? Spicy { get; set; }
    public int? Citrus { get; set; }
    public int? Floral { get; set; }
    public int? Fruity { get; set; }
    public int? Powdery { get; set; }
    public int? Smoky { get; set; }
    public int? Oriental { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// JSON array of preferred note IDs.
    /// </summary>
    public string? PreferredNoteIds { get; set; }

    /// <summary>When true, behavioral signals (favorites, ratings, journal) influence recommendations.</summary>
    public bool UseBehavioralSignals { get; set; } = true;

    /// <summary>UTC timestamp of last personalization reset (behavioral signals ignored before this).</summary>
    public DateTime? PersonalizationResetAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Season? PreferredSeason { get; set; }
    public FragranceType? PreferredFragranceType { get; set; }
}
