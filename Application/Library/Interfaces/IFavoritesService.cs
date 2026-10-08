using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Library.Interfaces;

public interface IFavoritesService
{
    Task<PagedResult<PerfumeListItemDto>> GetFavoritesAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task AddFavoriteAsync(int userId, int perfumeId, CancellationToken cancellationToken = default);

    Task RemoveFavoriteAsync(int userId, int perfumeId, CancellationToken cancellationToken = default);
}
