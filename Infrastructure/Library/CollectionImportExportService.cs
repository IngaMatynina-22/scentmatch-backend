using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class CollectionImportExportService : ICollectionImportExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ScentMatchDbContext _dbContext;

    public CollectionImportExportService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(string Content, string ContentType, string FileName)> ExportAsync(
        int userId,
        string format,
        CancellationToken cancellationToken = default)
    {
        var normalizedFormat = (format ?? "json").Trim().ToLowerInvariant();
        if (normalizedFormat is not ("json" or "csv"))
        {
            throw new BadRequestException("format must be json or csv.");
        }

        var collections = await _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Name,
                x.Description,
                x.ScenarioKey,
                PerfumeIds = x.CollectionPerfumes
                    .Where(cp => cp.Perfume.IsActive && cp.Perfume.Brand.IsActive)
                    .Select(cp => cp.PerfumeId)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var document = new CollectionExportDocument
        {
            Collections = collections.Select(x => new CollectionExportItem
            {
                Name = x.Name,
                Description = x.Description,
                ScenarioKey = x.ScenarioKey,
                PerfumeIds = x.PerfumeIds
            }).ToList()
        };

        if (normalizedFormat == "json")
        {
            var json = JsonSerializer.Serialize(document, JsonOptions);
            return (json, "application/json", "collections-export.json");
        }

        var csv = BuildCsv(document);
        return (csv, "text/csv", "collections-export.csv");
    }

    public async Task<CollectionImportResult> ImportJsonAsync(
        int userId,
        CollectionExportDocument document,
        bool preview,
        CancellationToken cancellationToken = default)
    {
        if (document.Collections is null)
        {
            throw new BadRequestException("collections is required.");
        }

        var rows = ExpandToRows(document);
        if (rows.Count > CollectionImportLimits.MaxRows)
        {
            throw new BadRequestException(
                $"Import may contain at most {CollectionImportLimits.MaxRows} rows.");
        }

        return await ImportRowsAsync(userId, rows, preview, cancellationToken);
    }

    public async Task<CollectionImportResult> ImportCsvAsync(
        int userId,
        Stream csvStream,
        bool preview,
        CancellationToken cancellationToken = default)
    {
        if (csvStream.CanSeek)
        {
            if (csvStream.Length > CollectionImportLimits.MaxFileBytes)
            {
                throw new BadRequestException(
                    $"CSV file must be at most {CollectionImportLimits.MaxFileBytes} bytes.");
            }
        }

        using var limited = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;
        int read;
        while ((read = await csvStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            total += read;
            if (total > CollectionImportLimits.MaxFileBytes)
            {
                throw new BadRequestException(
                    $"CSV file must be at most {CollectionImportLimits.MaxFileBytes} bytes.");
            }

            await limited.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        limited.Position = 0;
        using var reader = new StreamReader(limited, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = ParseCsv(reader);

        if (rows.Count > CollectionImportLimits.MaxRows)
        {
            throw new BadRequestException(
                $"Import may contain at most {CollectionImportLimits.MaxRows} rows.");
        }

        return await ImportRowsAsync(userId, rows, preview, cancellationToken);
    }

    private async Task<CollectionImportResult> ImportRowsAsync(
        int userId,
        IReadOnlyList<ImportRow> rows,
        bool preview,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var unknownPerfumes = new HashSet<int>();
        var imported = 0;
        var skipped = 0;
        var duplicates = 0;

        var perfumeIds = rows.Select(x => x.PerfumeId).Where(id => id > 0).Distinct().ToList();
        var existingPerfumeIds = perfumeIds.Count == 0
            ? new HashSet<int>()
            : (await _dbContext.Perfumes
                .AsNoTracking()
                .Where(x => perfumeIds.Contains(x.Id) && x.IsActive && x.Brand.IsActive)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var existingCollections = await _dbContext.Collections
            .Include(x => x.CollectionPerfumes)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var collectionsByName = existingCollections
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var pendingAdds = new List<(Collection Collection, int PerfumeId)>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 1;

            if (string.IsNullOrWhiteSpace(row.CollectionName))
            {
                errors.Add($"Row {rowNumber}: collection name is required.");
                skipped++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.ScenarioKey) && !WardrobeScenarios.IsValid(row.ScenarioKey))
            {
                errors.Add($"Row {rowNumber}: invalid scenarioKey '{row.ScenarioKey}'.");
                skipped++;
                continue;
            }

            var name = row.CollectionName.Trim();
            if (!collectionsByName.TryGetValue(name, out var collection))
            {
                collection = new Collection
                {
                    UserId = userId,
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
                    ScenarioKey = string.IsNullOrWhiteSpace(row.ScenarioKey)
                        ? null
                        : WardrobeScenarios.Normalize(row.ScenarioKey),
                    CreatedAt = DateTime.UtcNow
                };

                if (!preview)
                {
                    _dbContext.Collections.Add(collection);
                }

                collectionsByName[name] = collection;
                imported++;
            }

            if (row.PerfumeId <= 0)
            {
                continue;
            }

            if (!existingPerfumeIds.Contains(row.PerfumeId))
            {
                unknownPerfumes.Add(row.PerfumeId);
                skipped++;
                continue;
            }

            var alreadyLinked = collection.Id > 0
                && collection.CollectionPerfumes.Any(cp => cp.PerfumeId == row.PerfumeId);

            var pendingDuplicate = pendingAdds.Any(x =>
                ReferenceEquals(x.Collection, collection) && x.PerfumeId == row.PerfumeId);

            if (alreadyLinked || pendingDuplicate)
            {
                duplicates++;
                continue;
            }

            pendingAdds.Add((collection, row.PerfumeId));
            imported++;
        }

        if (!preview && (pendingAdds.Count > 0 || _dbContext.ChangeTracker.HasChanges()))
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var (collection, perfumeId) in pendingAdds)
            {
                _dbContext.CollectionPerfumes.Add(new CollectionPerfume
                {
                    CollectionId = collection.Id,
                    PerfumeId = perfumeId,
                    AddedAt = DateTime.UtcNow
                });
                collection.UpdatedAt = DateTime.UtcNow;
            }

            if (pendingAdds.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return new CollectionImportResult
        {
            Preview = preview,
            Imported = imported,
            Skipped = skipped,
            Duplicates = duplicates,
            UnknownPerfumes = unknownPerfumes.OrderBy(x => x).ToList(),
            Errors = errors
        };
    }

    private static List<ImportRow> ExpandToRows(CollectionExportDocument document)
    {
        var rows = new List<ImportRow>();
        foreach (var collection in document.Collections)
        {
            if (collection.PerfumeIds is null || collection.PerfumeIds.Count == 0)
            {
                rows.Add(new ImportRow
                {
                    CollectionName = collection.Name,
                    Description = collection.Description,
                    ScenarioKey = collection.ScenarioKey,
                    PerfumeId = 0
                });
                continue;
            }

            foreach (var perfumeId in collection.PerfumeIds)
            {
                rows.Add(new ImportRow
                {
                    CollectionName = collection.Name,
                    Description = collection.Description,
                    ScenarioKey = collection.ScenarioKey,
                    PerfumeId = perfumeId
                });
            }
        }

        return rows;
    }

    private static List<ImportRow> ParseCsv(TextReader reader)
    {
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            throw new BadRequestException("CSV is empty.");
        }

        var headers = SplitCsvLine(headerLine)
            .Select(h => h.Trim().TrimStart('\'').ToLowerInvariant())
            .ToList();

        var nameIdx = IndexOf(headers, "collectionname", "name");
        var descIdx = IndexOf(headers, "description");
        var scenarioIdx = IndexOf(headers, "scenariokey", "scenario");
        var perfumeIdx = IndexOf(headers, "perfumeid");

        if (nameIdx < 0 || perfumeIdx < 0)
        {
            throw new BadRequestException(
                "CSV must include CollectionName and PerfumeId columns.");
        }

        var rows = new List<ImportRow>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = SplitCsvLine(line);
            rows.Add(new ImportRow
            {
                CollectionName = UnescapeCsvCell(GetCell(cells, nameIdx)),
                Description = UnescapeCsvCell(GetCell(cells, descIdx)),
                ScenarioKey = UnescapeCsvCell(GetCell(cells, scenarioIdx)),
                PerfumeId = int.TryParse(
                    UnescapeCsvCell(GetCell(cells, perfumeIdx)),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var perfumeId)
                    ? perfumeId
                    : 0
            });
        }

        return rows;
    }

    private static string BuildCsv(CollectionExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CollectionName,Description,ScenarioKey,PerfumeId");

        foreach (var collection in document.Collections)
        {
            if (collection.PerfumeIds.Count == 0)
            {
                sb.Append(EscapeCsv(collection.Name)).Append(',')
                    .Append(EscapeCsv(collection.Description)).Append(',')
                    .Append(EscapeCsv(collection.ScenarioKey)).Append(',')
                    .AppendLine(EscapeCsv(string.Empty));
                continue;
            }

            foreach (var perfumeId in collection.PerfumeIds)
            {
                sb.Append(EscapeCsv(collection.Name)).Append(',')
                    .Append(EscapeCsv(collection.Description)).Append(',')
                    .Append(EscapeCsv(collection.ScenarioKey)).Append(',')
                    .AppendLine(EscapeCsv(perfumeId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        var cell = value ?? string.Empty;
        if (cell.Length > 0 && "=+-@".Contains(cell[0]))
        {
            cell = "'" + cell;
        }

        if (cell.Contains('"') || cell.Contains(',') || cell.Contains('\n') || cell.Contains('\r'))
        {
            return $"\"{cell.Replace("\"", "\"\"")}\"";
        }

        return cell;
    }

    private static string? UnescapeCsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.StartsWith('\'') && value.Length > 1 && "=+-@".Contains(value[1])
            ? value[1..]
            : value;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private static int IndexOf(IReadOnlyList<string> headers, params string[] names)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (names.Any(n => headers[i].Equals(n, StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? GetCell(IReadOnlyList<string> cells, int index)
    {
        return index < 0 || index >= cells.Count ? null : cells[index];
    }

    private sealed class ImportRow
    {
        public string? CollectionName { get; set; }
        public string? Description { get; set; }
        public string? ScenarioKey { get; set; }
        public int PerfumeId { get; set; }
    }
}
