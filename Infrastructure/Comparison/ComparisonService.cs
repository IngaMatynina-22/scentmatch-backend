using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Comparison.DTOs;
using ScentMatch.Application.Comparison.Interfaces;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.Library;

namespace ScentMatch.Infrastructure.Comparison;

public class ComparisonService : IComparisonService
{
    private readonly ScentMatchDbContext _dbContext;

    public ComparisonService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ComparisonResponse> CompareAsync(
        ComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfumeIds = request.PerfumeIds;

        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (perfumes.Count != perfumeIds.Count)
        {
            // Same response for missing and inactive — do not leak catalog internals.
            throw new NotFoundException("One or more perfumes were not found.");
        }

        var perfumeMap = perfumes.ToDictionary(x => x.Id);
        var items = perfumeIds
            .Select(id => CatalogMappings.ToDetailsDto(perfumeMap[id]))
            .ToList();

        return new ComparisonResponse { Items = items };
    }
}
