using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Recommendations.DTOs;

namespace ScentMatch.Application.Recommendations.Interfaces;

public interface IRecommendationHistoryService
{
    Task<PagedResult<RecommendationHistorySummaryDto>> GetBatchesAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task<RecommendationHistoryDetailsDto> GetBatchByIdAsync(
        int userId,
        Guid batchId,
        CancellationToken cancellationToken = default);
}
