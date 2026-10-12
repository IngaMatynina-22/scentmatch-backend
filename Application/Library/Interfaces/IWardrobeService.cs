using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Interfaces;

public interface IWardrobeService
{
    Task<WardrobeResponse> GetWardrobeAsync(int userId, CancellationToken cancellationToken = default);

    Task<WardrobeStatsDto> GetStatsAsync(int userId, CancellationToken cancellationToken = default);

    Task AssignAsync(int userId, AssignWardrobeRequest request, CancellationToken cancellationToken = default);

    Task RemovePerfumeAsync(
        int userId,
        string scenarioKey,
        int perfumeId,
        CancellationToken cancellationToken = default);
}
