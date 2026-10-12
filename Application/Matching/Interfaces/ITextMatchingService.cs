using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Interfaces;

public interface ITextMatchingService
{
    Task<TextMatchingResponse> MatchAsync(
        TextMatchingRequest request,
        CancellationToken cancellationToken = default);
}
