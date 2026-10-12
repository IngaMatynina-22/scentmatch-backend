using System.ComponentModel;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Library.DTOs;

public class WishlistItemDto
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public PerfumeListItemDto? Perfume { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }
}

public class CreateWishlistItemRequest
{
    public int PerfumeId { get; set; }
    public string? Status { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
}

public class UpdateWishlistItemRequest
{
    public string Status { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
}

public class WishlistQueryParameters : PaginationQuery
{
    [Description("Filter by status")]
    public string? Status { get; set; }
}
