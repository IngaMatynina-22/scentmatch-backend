using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Library.DTOs;

public class CreateBlindComparisonRequest
{
    public IReadOnlyList<int> PerfumeIds { get; set; } = Array.Empty<int>();
}

public class BlindComparisonSessionDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? WinnerLabel { get; set; }
    public IReadOnlyList<BlindComparisonSlotDto> Slots { get; set; } =
        Array.Empty<BlindComparisonSlotDto>();
}

/// <summary>Active-session slot view: labels only (no perfume identity).</summary>
public class BlindComparisonSlotDto
{
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class BlindComparisonRatingsRequest
{
    public IReadOnlyList<BlindComparisonRatingItem> Ratings { get; set; } =
        Array.Empty<BlindComparisonRatingItem>();
}

public class BlindComparisonRatingItem
{
    public string Label { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class CompleteBlindComparisonRequest
{
    public string? WinnerLabel { get; set; }
}

public class BlindComparisonResultsDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? WinnerLabel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyList<BlindComparisonRevealedSlotDto> Slots { get; set; } =
        Array.Empty<BlindComparisonRevealedSlotDto>();
}

public class BlindComparisonRevealedSlotDto
{
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int PerfumeId { get; set; }
    public PerfumeListItemDto? Perfume { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}
