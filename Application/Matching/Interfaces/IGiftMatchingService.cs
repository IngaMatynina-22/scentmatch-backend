using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Interfaces;

public interface IGiftMatchingService
{
    Task<GiftMatchingResponse> MatchAsync(
        GiftMatchingRequest request,
        CancellationToken cancellationToken = default);
}
