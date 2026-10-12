using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Admin;

public class AdminCatalogService : IAdminCatalogService
{
    private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);

    private readonly ScentMatchDbContext _dbContext;
    private readonly IAdminAuditService _auditService;

    public AdminCatalogService(ScentMatchDbContext dbContext, IAdminAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<CatalogQualityReportDto> GetQualityReportAsync(CancellationToken cancellationToken = default)
    {
        var perfumes = await _dbContext.Perfumes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                BrandName = x.Brand.Name,
                x.ImageUrl,
                x.Intensity,
                x.Barcode,
                HasNotes = x.PerfumeNotes.Any(),
                HasSeasons = x.PerfumeSeasons.Any(),
                ShopLinks = x.ShopLinks.Select(s => new { s.Id, s.Url }).ToList()
            })
            .ToListAsync(cancellationToken);

        var issues = new List<CatalogQualityIssueDto>();

        foreach (var perfume in perfumes)
        {
            if (!perfume.HasNotes)
            {
                issues.Add(Issue(perfume.Id, perfume.Name, perfume.BrandName, "MissingNotes", "Perfume has no notes."));
            }

            if (!perfume.HasSeasons)
            {
                issues.Add(Issue(perfume.Id, perfume.Name, perfume.BrandName, "MissingSeasons", "Perfume has no seasons."));
            }

            if (string.IsNullOrWhiteSpace(perfume.ImageUrl))
            {
                issues.Add(Issue(perfume.Id, perfume.Name, perfume.BrandName, "MissingImage", "Perfume has no image."));
            }

            if (!perfume.Intensity.HasValue)
            {
                issues.Add(Issue(perfume.Id, perfume.Name, perfume.BrandName, "MissingIntensity", "Perfume intensity is not set."));
            }

            if (string.IsNullOrWhiteSpace(perfume.Barcode))
            {
                issues.Add(Issue(perfume.Id, perfume.Name, perfume.BrandName, "MissingBarcode", "Perfume barcode is not set."));
            }

            foreach (var link in perfume.ShopLinks)
            {
                if (!IsValidHttpUrl(link.Url))
                {
                    issues.Add(Issue(
                        perfume.Id,
                        perfume.Name,
                        perfume.BrandName,
                        "InvalidShopUrl",
                        $"Shop link {link.Id} has an invalid URL."));
                }
            }
        }

        var duplicateGroups = FindDuplicateGroups(perfumes.Select(p => (
            p.Id,
            p.Name,
            p.BrandName,
            (string?)p.Barcode)).ToList());

        foreach (var group in duplicateGroups)
        {
            foreach (var item in group.Items)
            {
                issues.Add(Issue(
                    item.PerfumeId,
                    item.Name,
                    item.BrandName,
                    "SuspectedDuplicate",
                    $"{group.Reason} (confidence {group.Confidence:0.00})."));
            }
        }

        return new CatalogQualityReportDto
        {
            TotalActivePerfumes = perfumes.Count,
            Issues = issues
                .OrderBy(x => x.IssueType)
                .ThenBy(x => x.PerfumeId)
                .ToList(),
            Summary = new CatalogQualitySummaryDto
            {
                MissingNotes = issues.Count(x => x.IssueType == "MissingNotes"),
                MissingSeasons = issues.Count(x => x.IssueType == "MissingSeasons"),
                MissingImage = issues.Count(x => x.IssueType == "MissingImage"),
                InvalidShopUrls = issues.Count(x => x.IssueType == "InvalidShopUrl"),
                MissingIntensity = issues.Count(x => x.IssueType == "MissingIntensity"),
                MissingBarcode = issues.Count(x => x.IssueType == "MissingBarcode"),
                SuspectedDuplicates = duplicateGroups.Sum(x => x.Items.Count)
            }
        };
    }

    public async Task<IReadOnlyList<CatalogDuplicateGroupDto>> GetDuplicatesAsync(
        CancellationToken cancellationToken = default)
    {
        var perfumes = await _dbContext.Perfumes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                BrandName = x.Brand.Name,
                x.Barcode
            })
            .ToListAsync(cancellationToken);

        return FindDuplicateGroups(perfumes.Select(p => (
            p.Id,
            p.Name,
            p.BrandName,
            p.Barcode)).ToList());
    }

    public async Task<CatalogImportResultDto> ImportAsync(
        CatalogImportRequest request,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var brands = await _dbContext.Brands
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var fragranceTypes = await _dbContext.FragranceTypes.ToListAsync(cancellationToken);
        var notes = await _dbContext.Notes.ToListAsync(cancellationToken);
        var seasons = await _dbContext.Seasons.ToListAsync(cancellationToken);

        var activePerfumes = await _dbContext.Perfumes
            .Include(x => x.Brand)
            .Include(x => x.PerfumeNotes)
            .Include(x => x.PerfumeSeasons)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var rowResults = new List<CatalogImportRowResultDto>();
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;

        var defaultFragranceType = fragranceTypes
            .OrderBy(x => x.Id)
            .FirstOrDefault();

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];
            try
            {
                var result = await ProcessImportRowAsync(
                    i,
                    row,
                    request.Apply,
                    brands,
                    fragranceTypes,
                    notes,
                    seasons,
                    activePerfumes,
                    defaultFragranceType,
                    cancellationToken);

                rowResults.Add(result);

                switch (result.Status)
                {
                    case "Created":
                        created++;
                        break;
                    case "Updated":
                        updated++;
                        break;
                    case "WouldCreate":
                    case "WouldUpdate":
                    case "Skipped":
                        skipped++;
                        break;
                    default:
                        failed++;
                        break;
                }
            }
            catch (Exception ex)
            {
                failed++;
                rowResults.Add(new CatalogImportRowResultDto
                {
                    Index = i,
                    Status = "Failed",
                    Message = SanitizeImportError(ex.Message)
                });
            }
        }

        if (request.Apply)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var importResult = new CatalogImportResultDto
        {
            Applied = request.Apply,
            TotalRows = request.Rows.Count,
            Created = created,
            Updated = updated,
            Skipped = skipped,
            Failed = failed,
            Rows = rowResults
        };

        await _auditService.LogAsync(
            adminUserId,
            request.Apply ? "CatalogImportApply" : "CatalogImportPreview",
            "CatalogImport",
            null,
            failed > 0 ? "PartialSuccess" : "Success",
            $"rows={importResult.TotalRows}; created={created}; updated={updated}; skipped={skipped}; failed={failed}",
            cancellationToken);

        return importResult;
    }

    private async Task<CatalogImportRowResultDto> ProcessImportRowAsync(
        int index,
        CatalogImportRowDto row,
        bool apply,
        List<Brand> brands,
        List<FragranceType> fragranceTypes,
        List<Note> notes,
        List<Season> seasons,
        List<Perfume> activePerfumes,
        FragranceType? defaultFragranceType,
        CancellationToken cancellationToken)
    {
        var name = row.Name.Trim();
        var brandName = row.BrandName.Trim();
        var barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim();

        Perfume? existing = null;
        string? matchedBy = null;

        if (barcode is not null)
        {
            existing = activePerfumes.FirstOrDefault(x => x.Barcode == barcode);
            if (existing is not null)
            {
                matchedBy = "barcode";
            }
        }

        if (existing is null)
        {
            var key = NormalizeKey(name, brandName);
            existing = activePerfumes.FirstOrDefault(x =>
                NormalizeKey(x.Name, x.Brand.Name) == key);
            if (existing is not null)
            {
                matchedBy = "brand+name";
            }
        }

        var isDuplicateCandidate = existing is null
            && activePerfumes.Any(x =>
                string.Equals(NormalizeName(x.Name), NormalizeName(name), StringComparison.Ordinal)
                && !string.Equals(NormalizeName(x.Brand.Name), NormalizeName(brandName), StringComparison.Ordinal));

        Brand? brand = brands.FirstOrDefault(x =>
            string.Equals(x.Name, brandName, StringComparison.OrdinalIgnoreCase));

        if (brand is null)
        {
            if (!apply)
            {
                return new CatalogImportRowResultDto
                {
                    Index = index,
                    Status = existing is null ? "WouldCreate" : "WouldUpdate",
                    PerfumeId = existing?.Id,
                    MatchedBy = matchedBy,
                    Message = existing is null
                        ? $"Brand '{brandName}' would be created with a new perfume."
                        : $"Would update existing perfume and create brand '{brandName}'.",
                    IsDuplicateCandidate = isDuplicateCandidate
                };
            }

            brand = new Brand
            {
                Name = brandName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Brands.Add(brand);
            brands.Add(brand);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        FragranceType? fragranceType = null;
        if (!string.IsNullOrWhiteSpace(row.FragranceTypeName))
        {
            fragranceType = fragranceTypes.FirstOrDefault(x =>
                string.Equals(x.Name, row.FragranceTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        fragranceType ??= defaultFragranceType;
        if (fragranceType is null)
        {
            return new CatalogImportRowResultDto
            {
                Index = index,
                Status = "Failed",
                Message = "No fragrance type available for import.",
                IsDuplicateCandidate = isDuplicateCandidate
            };
        }

        if (existing is not null)
        {
            if (!apply)
            {
                return new CatalogImportRowResultDto
                {
                    Index = index,
                    Status = "WouldUpdate",
                    PerfumeId = existing.Id,
                    MatchedBy = matchedBy,
                    Message = $"Would update existing perfume matched by {matchedBy}.",
                    IsDuplicateCandidate = false
                };
            }

            existing.Name = name;
            existing.BrandId = brand.Id;
            existing.FragranceTypeId = fragranceType.Id;
            existing.Price = row.Price ?? existing.Price;
            existing.Currency = string.IsNullOrWhiteSpace(row.Currency)
                ? existing.Currency
                : row.Currency.Trim().ToUpperInvariant();
            existing.Barcode = barcode ?? existing.Barcode;
            existing.ImageUrl = string.IsNullOrWhiteSpace(row.ImageUrl) ? existing.ImageUrl : row.ImageUrl.Trim();
            existing.Description = string.IsNullOrWhiteSpace(row.Description) ? existing.Description : row.Description.Trim();
            existing.Intensity = row.Intensity ?? existing.Intensity;
            existing.UpdatedAt = DateTime.UtcNow;

            ApplyNotesAndSeasons(existing, row, notes, seasons);

            return new CatalogImportRowResultDto
            {
                Index = index,
                Status = "Updated",
                PerfumeId = existing.Id,
                MatchedBy = matchedBy,
                Message = $"Updated perfume matched by {matchedBy}."
            };
        }

        if (!apply)
        {
            return new CatalogImportRowResultDto
            {
                Index = index,
                Status = "WouldCreate",
                Message = "Would create new perfume.",
                IsDuplicateCandidate = isDuplicateCandidate
            };
        }

        var perfume = new Perfume
        {
            Name = name,
            BrandId = brand.Id,
            Brand = brand,
            FragranceTypeId = fragranceType.Id,
            Price = row.Price,
            Currency = string.IsNullOrWhiteSpace(row.Currency) ? null : row.Currency.Trim().ToUpperInvariant(),
            Barcode = barcode,
            ImageUrl = string.IsNullOrWhiteSpace(row.ImageUrl) ? null : row.ImageUrl.Trim(),
            Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
            Intensity = row.Intensity,
            Rating = 0,
            ReviewsCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Perfumes.Add(perfume);
        await _dbContext.SaveChangesAsync(cancellationToken);

        ApplyNotesAndSeasons(perfume, row, notes, seasons);
        activePerfumes.Add(perfume);

        return new CatalogImportRowResultDto
        {
            Index = index,
            Status = "Created",
            PerfumeId = perfume.Id,
            Message = "Created perfume.",
            IsDuplicateCandidate = isDuplicateCandidate
        };
    }

    private void ApplyNotesAndSeasons(
        Perfume perfume,
        CatalogImportRowDto row,
        List<Note> notes,
        List<Season> seasons)
    {
        if (row.NoteNames.Count > 0)
        {
            var existingNoteIds = perfume.PerfumeNotes.Select(x => x.NoteId).ToHashSet();
            foreach (var noteName in row.NoteNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var note = notes.FirstOrDefault(x =>
                    string.Equals(x.Name, noteName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (note is null || existingNoteIds.Contains(note.Id))
                {
                    continue;
                }

                _dbContext.PerfumeNotes.Add(new PerfumeNote
                {
                    PerfumeId = perfume.Id,
                    NoteId = note.Id,
                    NoteType = NoteTypes.Middle
                });
                existingNoteIds.Add(note.Id);
            }
        }

        if (row.SeasonNames.Count > 0)
        {
            var existingSeasonIds = perfume.PerfumeSeasons.Select(x => x.SeasonId).ToHashSet();
            foreach (var seasonName in row.SeasonNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var season = seasons.FirstOrDefault(x =>
                    string.Equals(x.Name, seasonName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (season is null || existingSeasonIds.Contains(season.Id))
                {
                    continue;
                }

                _dbContext.PerfumeSeasons.Add(new PerfumeSeason
                {
                    PerfumeId = perfume.Id,
                    SeasonId = season.Id,
                    Suitability = 50
                });
                existingSeasonIds.Add(season.Id);
            }
        }
    }

    private static IReadOnlyList<CatalogDuplicateGroupDto> FindDuplicateGroups(
        IReadOnlyList<(int Id, string Name, string BrandName, string? Barcode)> perfumes)
    {
        var groups = new List<CatalogDuplicateGroupDto>();

        var byBarcode = perfumes
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .GroupBy(x => x.Barcode!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var g in byBarcode)
        {
            groups.Add(new CatalogDuplicateGroupDto
            {
                NormalizedKey = $"barcode:{g.Key}",
                Reason = "Identical barcode",
                Confidence = 1.0m,
                Items = g.Select(x => new CatalogDuplicateItemDto
                {
                    PerfumeId = x.Id,
                    Name = x.Name,
                    BrandName = x.BrandName,
                    Barcode = x.Barcode
                }).ToList()
            });
        }

        var byNameBrand = perfumes
            .GroupBy(x => NormalizeKey(x.Name, x.BrandName))
            .Where(g => g.Count() > 1);

        foreach (var g in byNameBrand)
        {
            groups.Add(new CatalogDuplicateGroupDto
            {
                NormalizedKey = $"name+brand:{g.Key}",
                Reason = "Normalized name and brand match",
                Confidence = 0.9m,
                Items = g.Select(x => new CatalogDuplicateItemDto
                {
                    PerfumeId = x.Id,
                    Name = x.Name,
                    BrandName = x.BrandName,
                    Barcode = x.Barcode
                }).ToList()
            });
        }

        return groups
            .OrderByDescending(x => x.Confidence)
            .ThenBy(x => x.NormalizedKey)
            .ToList();
    }

    private static CatalogQualityIssueDto Issue(
        int perfumeId,
        string perfumeName,
        string brandName,
        string issueType,
        string message) => new()
    {
        PerfumeId = perfumeId,
        PerfumeName = perfumeName,
        BrandName = brandName,
        IssueType = issueType,
        Message = message
    };

    private static bool IsValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string NormalizeName(string value) =>
        MultiWhitespace.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), " ");

    private static string NormalizeKey(string name, string brandName) =>
        $"{NormalizeName(brandName)}|{NormalizeName(name)}";

    private static string SanitizeImportError(string message)
    {
        if (message.Contains("password", StringComparison.OrdinalIgnoreCase)
            || message.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            return "Import row failed (details redacted).";
        }

        return message.Length <= 500 ? message : message[..497] + "...";
    }
}
