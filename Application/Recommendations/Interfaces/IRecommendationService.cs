using ScentMatch.Application.Recommendations.DTOs;

namespace ScentMatch.Application.Recommendations.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationsResponse> GetRecommendationsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<RecommendationsResponse> RefreshRecommendationsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
