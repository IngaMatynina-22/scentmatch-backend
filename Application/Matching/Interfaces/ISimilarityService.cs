using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Interfaces;

public interface ISimilarityService
{
    Task<PagedResult<SimilarPerfumeDto>> GetSimilarAsync(
        int perfumeId,
        SimilarPerfumesQuery query,
        CancellationToken cancellationToken = default);
}
