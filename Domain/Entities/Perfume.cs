namespace ScentMatch.Domain.Entities;

public class Perfume
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public int FragranceTypeId { get; set; }
    public string? Description { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Concentration { get; set; }
    public string? Gender { get; set; }
    public decimal? Price { get; set; }
    /// <summary>ISO 4217 currency for catalog Price. Null means currency unknown, not zero.</summary>
    public string? Currency { get; set; }
    /// <summary>Bottle volume in milliliters when known.</summary>
    public decimal? VolumeMl { get; set; }
    /// <summary>Overall intensity 0–100. Null means unknown, not zero intensity.</summary>
    public int? Intensity { get; set; }
    /// <summary>Unique product barcode (EAN/UPC) when assigned.</summary>
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Rating { get; set; }
    public int ReviewsCount { get; set; }

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

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Brand Brand { get; set; } = null!;
    public FragranceType FragranceType { get; set; } = null!;
    public ICollection<PerfumeNote> PerfumeNotes { get; set; } = new List<PerfumeNote>();
    public ICollection<PerfumeSeason> PerfumeSeasons { get; set; } = new List<PerfumeSeason>();
    public ICollection<PerfumeShopLink> ShopLinks { get; set; } = new List<PerfumeShopLink>();
    public ICollection<FavoritePerfume> FavoritePerfumes { get; set; } = new List<FavoritePerfume>();
    public ICollection<CollectionPerfume> CollectionPerfumes { get; set; } = new List<CollectionPerfume>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ViewHistory> ViewHistory { get; set; } = new List<ViewHistory>();
    public ICollection<RecommendationHistory> RecommendationHistory { get; set; } = new List<RecommendationHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ScentJournalEntry> ScentJournalEntries { get; set; } = new List<ScentJournalEntry>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<PriceWatch> PriceWatches { get; set; } = new List<PriceWatch>();
    public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
}
