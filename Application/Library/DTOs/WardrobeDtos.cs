using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Library.DTOs;

public class WardrobeResponse
{
    public IReadOnlyList<WardrobeScenarioGroupDto> Scenarios { get; set; } =
        Array.Empty<WardrobeScenarioGroupDto>();
}

public class WardrobeScenarioGroupDto
{
    public string ScenarioKey { get; set; } = string.Empty;
    public int PerfumeCount { get; set; }
    public IReadOnlyList<WardrobeCollectionDto> Collections { get; set; } =
        Array.Empty<WardrobeCollectionDto>();
}

public class WardrobeCollectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<PerfumeListItemDto> Perfumes { get; set; } =
        Array.Empty<PerfumeListItemDto>();
}

public class WardrobeStatsDto
{
    public IReadOnlyList<WardrobeScenarioStatDto> Scenarios { get; set; } =
        Array.Empty<WardrobeScenarioStatDto>();
}

public class WardrobeScenarioStatDto
{
    public string ScenarioKey { get; set; } = string.Empty;
    public int CollectionCount { get; set; }
    public int PerfumeCount { get; set; }
}

public class AssignWardrobeRequest
{
    public int PerfumeId { get; set; }
    public string ScenarioKey { get; set; } = string.Empty;
    public string? CollectionName { get; set; }
}
