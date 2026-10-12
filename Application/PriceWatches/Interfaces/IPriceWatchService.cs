using ScentMatch.Application.Common.Models;
using ScentMatch.Application.PriceWatches.DTOs;

namespace ScentMatch.Application.PriceWatches.Interfaces;

public interface IPriceWatchService
{
    Task<PriceWatchDto> CreateAsync(
        int userId,
        CreatePriceWatchRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PriceWatchDto>> ListAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task<PriceWatchDto> GetAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default);

    Task<PriceWatchDto> UpdateAsync(
        int userId,
        int id,
        UpdatePriceWatchRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceSnapshotDto>> GetHistoryAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default);
}
