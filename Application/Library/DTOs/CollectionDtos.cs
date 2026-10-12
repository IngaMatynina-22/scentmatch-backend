using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Library.DTOs;

public class CollectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ScenarioKey { get; set; }
    public int PerfumeCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CollectionDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ScenarioKey { get; set; }
    public int PerfumeCount { get; set; }
    public IReadOnlyList<PerfumeListItemDto> Perfumes { get; set; } = Array.Empty<PerfumeListItemDto>();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ScenarioKey { get; set; }
}

public class UpdateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ScenarioKey { get; set; }
}
