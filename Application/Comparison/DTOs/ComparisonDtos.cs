using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Comparison.DTOs;

public class ComparisonRequest
{
    public IReadOnlyList<int> PerfumeIds { get; set; } = Array.Empty<int>();
}

public class ComparisonResponse
{
    public IReadOnlyList<PerfumeDetailsDto> Items { get; set; } = Array.Empty<PerfumeDetailsDto>();
    public ComparisonAnalysisDto Analysis { get; set; } = new();
}

public class ComparisonAnalysisDto
{
    public IReadOnlyList<NamedEntityDto> CommonNotes { get; set; } = Array.Empty<NamedEntityDto>();
    public IReadOnlyList<NamedEntityDto> DifferingNotes { get; set; } = Array.Empty<NamedEntityDto>();
    public IReadOnlyList<NamedEntityDto> CommonSeasons { get; set; } = Array.Empty<NamedEntityDto>();
    public IReadOnlyList<NamedEntityDto> DifferingSeasons { get; set; } = Array.Empty<NamedEntityDto>();

    public ComparisonScalarFieldDto Brand { get; set; } = new();
    public ComparisonScalarFieldDto FragranceType { get; set; } = new();
    public ComparisonScalarFieldDto Concentration { get; set; } = new();
    public ComparisonScalarFieldDto Gender { get; set; } = new();
    public ComparisonNumericFieldDto Rating { get; set; } = new();
    public ComparisonNumericFieldDto Price { get; set; } = new();
    public ComparisonNumericFieldDto ReleaseYear { get; set; } = new();

    public IReadOnlyList<ComparisonCharacteristicFieldDto> Characteristics { get; set; } =
        Array.Empty<ComparisonCharacteristicFieldDto>();
}

public class ComparisonScalarFieldDto
{
    public bool IsCommon { get; set; }
    public IReadOnlyList<string?> Values { get; set; } = Array.Empty<string?>();
}

public class ComparisonNumericFieldDto
{
    public bool IsCommon { get; set; }
    public IReadOnlyList<decimal?> Values { get; set; } = Array.Empty<decimal?>();
}

public class ComparisonCharacteristicFieldDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsCommon { get; set; }
    public IReadOnlyList<int?> Values { get; set; } = Array.Empty<int?>();
}
