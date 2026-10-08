using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Interfaces;

public interface IMatchingService
{
    Task<MatchingResponse> MatchAsync(
        MatchingRequest request,
        CancellationToken cancellationToken = default);
}
