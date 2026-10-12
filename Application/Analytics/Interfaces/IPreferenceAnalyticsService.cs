using ScentMatch.Application.Analytics.DTOs;

namespace ScentMatch.Application.Analytics.Interfaces;

public interface IPreferenceAnalyticsService
{
    Task<PreferenceAnalyticsDto> GetPreferencesAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ResetPersonalizationResponse> ResetPersonalizationAsync(
        int userId,
        ResetPersonalizationRequest request,
        CancellationToken cancellationToken = default);
}
