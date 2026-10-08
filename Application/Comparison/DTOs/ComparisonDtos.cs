using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Comparison.DTOs;

public class ComparisonRequest
{
    public IReadOnlyList<int> PerfumeIds { get; set; } = Array.Empty<int>();
}

public class ComparisonResponse
{
    public IReadOnlyList<PerfumeDetailsDto> Items { get; set; } = Array.Empty<PerfumeDetailsDto>();
}
