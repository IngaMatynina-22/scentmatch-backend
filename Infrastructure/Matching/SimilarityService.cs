using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Matching;

public class SimilarityService : ISimilarityService
{
    private readonly ScentMatchDbContext _dbContext;

    public SimilarityService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<SimilarPerfumeDto>> GetSimilarAsync(
        int perfumeId,
        SimilarPerfumesQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = await PerfumeProfileLoader.LoadByIdAsync(_dbContext, perfumeId, cancellationToken);
        if (source is null)
        {
            throw new NotFoundException($"Perfume with id {perfumeId} was not found.");
        }

        var candidates = await PerfumeProfileLoader.LoadActiveAsync(
            _dbContext,
            cancellationToken,
            excludePerfumeId: perfumeId);

        var scored = candidates
            .Select(candidate =>
            {
                var breakdown = PerfumeSimilarityCalculator.Calculate(source, candidate);
                return new SimilarPerfumeDto
                {
                    Perfume = new PerfumeSummaryDto
                    {
                        Id = candidate.Id,
                        Name = candidate.Name,
                        BrandName = candidate.BrandName,
                        FragranceTypeName = candidate.FragranceTypeName,
                        Price = candidate.Price,
                        Currency = candidate.Currency,
                        ImageUrl = candidate.ImageUrl
                    },
                    TotalScore = breakdown.TotalScore,
                    Components = breakdown.Components,
                    PositiveReasons = breakdown.PositiveReasons,
                    MismatchReasons = breakdown.MismatchReasons,
                    DataGaps = breakdown.DataGaps
                };
            })
            .Where(x => x.TotalScore > 0m)
            .OrderByDescending(x => x.TotalScore)
            .ThenBy(x => x.Perfume.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Perfume.Id)
            .ToList();

        if (query.Limit.HasValue)
        {
            var limited = scored.Take(query.Limit.Value).ToList();
            return PagedResult<SimilarPerfumeDto>.Create(
                limited,
                page: 1,
                pageSize: query.Limit.Value,
                totalItems: limited.Count);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var total = scored.Count;
        var items = scored
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PagedResult<SimilarPerfumeDto>.Create(items, page, pageSize, total);
    }
}
