using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Interfaces;

public interface IWishlistService
{
    Task<PagedResult<WishlistItemDto>> GetItemsAsync(
        int userId,
        WishlistQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<WishlistItemDto> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default);

    Task<WishlistItemDto> AddAsync(
        int userId,
        CreateWishlistItemRequest request,
        CancellationToken cancellationToken = default);

    Task<WishlistItemDto> UpdateAsync(
        int userId,
        int id,
        UpdateWishlistItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default);
}
