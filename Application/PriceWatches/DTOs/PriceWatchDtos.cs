namespace ScentMatch.Application.PriceWatches.DTOs;

public class CreatePriceWatchRequest
{
    public int PerfumeId { get; set; }
    public decimal TargetPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public int? ShopLinkId { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceName { get; set; }
}

public class UpdatePriceWatchRequest
{
    public decimal? TargetPrice { get; set; }
    public bool? IsActive { get; set; }
    public string? Currency { get; set; }
    public int? ShopLinkId { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceName { get; set; }
}

public class PriceWatchDto
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public string PerfumeName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public int? ShopLinkId { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal TargetPrice { get; set; }
    public decimal? LastKnownPrice { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public decimal? LastNotifiedPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PriceSnapshotDto
{
    public long Id { get; set; }
    public int PerfumeId { get; set; }
    public int? PriceWatchId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal Price { get; set; }
    public DateTime CapturedAt { get; set; }
}
