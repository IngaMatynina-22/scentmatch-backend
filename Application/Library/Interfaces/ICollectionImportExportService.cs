using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Interfaces;

public static class CollectionImportLimits
{
    public const int MaxFileBytes = 1_048_576;
    public const int MaxRows = 500;
}

public interface ICollectionImportExportService
{
    Task<(string Content, string ContentType, string FileName)> ExportAsync(
        int userId,
        string format,
        CancellationToken cancellationToken = default);

    Task<CollectionImportResult> ImportJsonAsync(
        int userId,
        CollectionExportDocument document,
        bool preview,
        CancellationToken cancellationToken = default);

    Task<CollectionImportResult> ImportCsvAsync(
        int userId,
        Stream csvStream,
        bool preview,
        CancellationToken cancellationToken = default);
}
