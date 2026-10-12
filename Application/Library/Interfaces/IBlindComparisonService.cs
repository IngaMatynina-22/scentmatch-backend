using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Interfaces;

public interface IBlindComparisonService
{
    Task<BlindComparisonSessionDto> CreateAsync(
        int userId,
        CreateBlindComparisonRequest request,
        CancellationToken cancellationToken = default);

    Task<BlindComparisonSessionDto> GetSessionAsync(
        int userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<BlindComparisonSessionDto> RateAsync(
        int userId,
        Guid sessionId,
        BlindComparisonRatingsRequest request,
        CancellationToken cancellationToken = default);

    Task<BlindComparisonResultsDto> CompleteAsync(
        int userId,
        Guid sessionId,
        CompleteBlindComparisonRequest request,
        CancellationToken cancellationToken = default);

    Task<BlindComparisonResultsDto> GetResultsAsync(
        int userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
