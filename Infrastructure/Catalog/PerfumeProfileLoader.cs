using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Scoring;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Catalog;

internal static class PerfumeProfileLoader
{
    public static async Task<List<PerfumeProfile>> LoadActiveAsync(
        ScentMatchDbContext db,
        CancellationToken cancellationToken,
        int? excludePerfumeId = null)
    {
        var query = db.Perfumes
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.FragranceType)
            .Include(x => x.PerfumeNotes).ThenInclude(n => n.Note)
            .Include(x => x.PerfumeSeasons).ThenInclude(s => s.Season)
            .Where(x => x.IsActive && x.Brand.IsActive);

        if (excludePerfumeId.HasValue)
        {
            query = query.Where(x => x.Id != excludePerfumeId.Value);
        }

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public static async Task<PerfumeProfile?> LoadByIdAsync(
        ScentMatchDbContext db,
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var entity = await db.Perfumes
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.FragranceType)
            .Include(x => x.PerfumeNotes).ThenInclude(n => n.Note)
            .Include(x => x.PerfumeSeasons).ThenInclude(s => s.Season)
            .Where(x => x.Id == perfumeId && x.IsActive && x.Brand.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : Map(entity);
    }

    private static PerfumeProfile Map(Perfume entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        BrandId = entity.BrandId,
        BrandName = entity.Brand.Name,
        FragranceTypeId = entity.FragranceTypeId,
        FragranceTypeName = entity.FragranceType.Name,
        Concentration = entity.Concentration,
        Gender = entity.Gender,
        Price = entity.Price,
        Currency = entity.Currency,
        VolumeMl = entity.VolumeMl,
        Intensity = entity.Intensity,
        Barcode = entity.Barcode,
        ImageUrl = entity.ImageUrl,
        Sweetness = entity.Sweetness,
        Freshness = entity.Freshness,
        Woody = entity.Woody,
        Spicy = entity.Spicy,
        Citrus = entity.Citrus,
        Floral = entity.Floral,
        Fruity = entity.Fruity,
        Powdery = entity.Powdery,
        Smoky = entity.Smoky,
        Oriental = entity.Oriental,
        Notes = entity.PerfumeNotes
            .Select(n => new ProfileNote(n.NoteId, n.Note.Name, n.NoteType))
            .ToList(),
        Seasons = entity.PerfumeSeasons
            .Select(s => new ProfileSeason(s.SeasonId, s.Season.Name, s.Suitability))
            .ToList()
    };
}
