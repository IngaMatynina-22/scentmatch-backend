using Microsoft.EntityFrameworkCore;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

internal static class PerfumeQueryHelper
{
    public static IQueryable<Perfume> ActivePerfumesWithDetails(ScentMatchDbContext dbContext)
    {
        return dbContext.Perfumes
            .AsNoTracking()
            .Where(x => x.IsActive && x.Brand.IsActive)
            .Include(x => x.Brand)
            .Include(x => x.FragranceType)
            .Include(x => x.PerfumeNotes)
                .ThenInclude(x => x.Note)
            .Include(x => x.PerfumeSeasons)
                .ThenInclude(x => x.Season);
    }

    public static Task<bool> IsActivePerfumeAsync(
        ScentMatchDbContext dbContext,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        return dbContext.Perfumes
            .AsNoTracking()
            .AnyAsync(x => x.Id == perfumeId && x.IsActive && x.Brand.IsActive, cancellationToken);
    }
}
