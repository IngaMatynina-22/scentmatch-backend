namespace ScentMatch.Application.Admin.DTOs;

public class CatalogQualityReportDto
{
    public int TotalActivePerfumes { get; set; }
    public IReadOnlyList<CatalogQualityIssueDto> Issues { get; set; } = Array.Empty<CatalogQualityIssueDto>();
    public CatalogQualitySummaryDto Summary { get; set; } = new();
}

public class CatalogQualitySummaryDto
{
    public int MissingNotes { get; set; }
    public int MissingSeasons { get; set; }
    public int MissingImage { get; set; }
    public int InvalidShopUrls { get; set; }
    public int MissingIntensity { get; set; }
    public int MissingBarcode { get; set; }
    public int SuspectedDuplicates { get; set; }
}

public class CatalogQualityIssueDto
{
    public int PerfumeId { get; set; }
    public string PerfumeName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CatalogDuplicateGroupDto
{
    public string NormalizedKey { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public IReadOnlyList<CatalogDuplicateItemDto> Items { get; set; } = Array.Empty<CatalogDuplicateItemDto>();
}

public class CatalogDuplicateItemDto
{
    public int PerfumeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
}

public class CatalogImportRequest
{
    /// <summary>When false, validate and report only. When true, apply idempotent upserts.</summary>
    public bool Apply { get; set; }
    public List<CatalogImportRowDto> Rows { get; set; } = [];
}

public class CatalogImportRowDto
{
    public string Name { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? FragranceTypeName { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int? Intensity { get; set; }
    public List<string> NoteNames { get; set; } = [];
    public List<string> SeasonNames { get; set; } = [];
}

public class CatalogImportResultDto
{
    public bool Applied { get; set; }
    public int TotalRows { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public IReadOnlyList<CatalogImportRowResultDto> Rows { get; set; } = Array.Empty<CatalogImportRowResultDto>();
}

public class CatalogImportRowResultDto
{
    public int Index { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? PerfumeId { get; set; }
    public string? MatchedBy { get; set; }
    public bool IsDuplicateCandidate { get; set; }
}
