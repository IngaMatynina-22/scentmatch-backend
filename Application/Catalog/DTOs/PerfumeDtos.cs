namespace ScentMatch.Application.Catalog.DTOs;

public class NamedEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PerfumeCharacteristicsDto
{
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
}

public class PerfumeNoteDto
{
    public int NoteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NoteType { get; set; } = string.Empty;
}

public class PerfumeSeasonDto
{
    public int SeasonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Suitability { get; set; }
}

public class PerfumeNoteInputDto
{
    public int NoteId { get; set; }
    public string NoteType { get; set; } = string.Empty;
}

public class PerfumeSeasonInputDto
{
    public int SeasonId { get; set; }
    public int Suitability { get; set; }
}

public class PerfumeListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NamedEntityDto Brand { get; set; } = null!;
    public NamedEntityDto FragranceType { get; set; } = null!;
    public string? Description { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Concentration { get; set; }
    public string? Gender { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Rating { get; set; }
    public int ReviewsCount { get; set; }
    public PerfumeCharacteristicsDto Characteristics { get; set; } = null!;
    public IReadOnlyList<PerfumeNoteDto> Notes { get; set; } = Array.Empty<PerfumeNoteDto>();
    public IReadOnlyList<PerfumeSeasonDto> Seasons { get; set; } = Array.Empty<PerfumeSeasonDto>();
}

public class PerfumeDetailsDto : PerfumeListItemDto
{
}

public class CreatePerfumeRequest
{
    public string Name { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public int FragranceTypeId { get; set; }
    public string? Description { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Concentration { get; set; }
    public string? Gender { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public PerfumeCharacteristicsDto? Characteristics { get; set; }
    public List<PerfumeNoteInputDto> Notes { get; set; } = [];
    public List<PerfumeSeasonInputDto> Seasons { get; set; } = [];
}

public class UpdatePerfumeRequest
{
    public string Name { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public int FragranceTypeId { get; set; }
    public string? Description { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Concentration { get; set; }
    public string? Gender { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public PerfumeCharacteristicsDto? Characteristics { get; set; }
    public List<PerfumeNoteInputDto> Notes { get; set; } = [];
    public List<PerfumeSeasonInputDto> Seasons { get; set; } = [];
}
