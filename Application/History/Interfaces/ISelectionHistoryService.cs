using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.History.Interfaces;

public interface ISelectionHistoryService
{
    Task<SelectionHistoryDto> CreateAsync(
        int userId,
        CreateSelectionHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<SelectionHistoryDto> CreateFromMatchingAsync(
        int userId,
        MatchingRequest request,
        IReadOnlyList<MatchingResultDto> results,
        CancellationToken cancellationToken = default);

    Task<SelectionHistoryDto> CreateFromMatchingAsync(
        int userId,
        MatchingRequest request,
        IReadOnlyList<MatchingResultDto> results,
        string scenarioType,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SelectionHistoryDto>> GetAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task<SelectionHistoryDto> GetByIdAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default);

    Task ClearAsync(int userId, CancellationToken cancellationToken = default);
}
