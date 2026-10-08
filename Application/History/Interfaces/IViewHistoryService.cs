using ScentMatch.Application.Common.Models;
using ScentMatch.Application.History.DTOs;

namespace ScentMatch.Application.History.Interfaces;

public interface IViewHistoryService
{
    Task<PagedResult<ViewHistoryDto>> GetAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task RegisterViewAsync(int userId, int perfumeId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int historyId, CancellationToken cancellationToken = default);

    Task ClearAsync(int userId, CancellationToken cancellationToken = default);
}
