using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ScentMatchDbContext _dbContext;

    public AdminDashboardService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var perfumeCount = await _dbContext.Perfumes.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var brandCount = await _dbContext.Brands.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var noteCount = await _dbContext.Notes.AsNoTracking().CountAsync(cancellationToken);
        var seasonCount = await _dbContext.Seasons.AsNoTracking().CountAsync(cancellationToken);
        var fragranceTypeCount = await _dbContext.FragranceTypes.AsNoTracking().CountAsync(cancellationToken);

        var reviewsByStatus = await _dbContext.Reviews
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var missingNotes = await _dbContext.Perfumes
            .AsNoTracking()
            .CountAsync(x => x.IsActive && !x.PerfumeNotes.Any(), cancellationToken);

        var missingSeasons = await _dbContext.Perfumes
            .AsNoTracking()
            .CountAsync(x => x.IsActive && !x.PerfumeSeasons.Any(), cancellationToken);

        var missingImage = await _dbContext.Perfumes
            .AsNoTracking()
            .CountAsync(x => x.IsActive && (x.ImageUrl == null || x.ImageUrl == string.Empty), cancellationToken);

        var missingPrice = await _dbContext.Perfumes
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.Price == null, cancellationToken);

        var recentCutoff = DateTime.UtcNow.AddDays(-7);
        var recentJobErrorCount = await _dbContext.BackgroundJobLogs
            .AsNoTracking()
            .CountAsync(
                x => x.StartedAt >= recentCutoff
                     && (x.ErrorCount > 0
                         || x.Status == "Failed"
                         || x.Status == "CompletedWithErrors"),
                cancellationToken);

        return new AdminDashboardDto
        {
            PerfumeCount = perfumeCount,
            BrandCount = brandCount,
            NoteCount = noteCount,
            SeasonCount = seasonCount,
            FragranceTypeCount = fragranceTypeCount,
            ReviewsByStatus = new ReviewsByStatusDto
            {
                Pending = reviewsByStatus.FirstOrDefault(x => x.Status == ReviewStatuses.Pending)?.Count ?? 0,
                Approved = reviewsByStatus.FirstOrDefault(x => x.Status == ReviewStatuses.Approved)?.Count ?? 0,
                Rejected = reviewsByStatus.FirstOrDefault(x => x.Status == ReviewStatuses.Rejected)?.Count ?? 0
            },
            CatalogGaps = new CatalogGapsDto
            {
                MissingNotes = missingNotes,
                MissingSeasons = missingSeasons,
                MissingImage = missingImage,
                MissingPrice = missingPrice
            },
            RecentJobErrorCount = recentJobErrorCount
        };
    }
}
