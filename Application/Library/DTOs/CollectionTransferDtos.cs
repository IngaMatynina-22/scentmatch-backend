namespace ScentMatch.Application.Library.DTOs;

public class CollectionExportDocument
{
    public IReadOnlyList<CollectionExportItem> Collections { get; set; } =
        Array.Empty<CollectionExportItem>();
}

public class CollectionExportItem
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ScenarioKey { get; set; }
    public IReadOnlyList<int> PerfumeIds { get; set; } = Array.Empty<int>();
}

public class CollectionImportResult
{
    public bool Preview { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Duplicates { get; set; }
    public IReadOnlyList<int> UnknownPerfumes { get; set; } = Array.Empty<int>();
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
