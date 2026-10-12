namespace ScentMatch.Application.Catalog.DTOs;

public static class BarcodeLookupStatuses
{
    public const string Found = "found";
    public const string NotFound = "notFound";
    public const string Ambiguous = "ambiguous";
}

public class BarcodeLookupResultDto
{
    public string Status { get; set; } = BarcodeLookupStatuses.NotFound;
    public PerfumeDetailsDto? Perfume { get; set; }
    public IReadOnlyList<PerfumeListItemDto> Matches { get; set; } = Array.Empty<PerfumeListItemDto>();
}
