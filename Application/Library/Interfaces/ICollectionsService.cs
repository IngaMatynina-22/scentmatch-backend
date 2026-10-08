using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Interfaces;

public interface ICollectionsService
{
    Task<PagedResult<CollectionDto>> GetCollectionsAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task<CollectionDetailsDto> GetByIdAsync(int userId, int collectionId, CancellationToken cancellationToken = default);

    Task<CollectionDto> CreateAsync(int userId, CreateCollectionRequest request, CancellationToken cancellationToken = default);

    Task<CollectionDto> UpdateAsync(int userId, int collectionId, UpdateCollectionRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int collectionId, CancellationToken cancellationToken = default);

    Task AddPerfumeAsync(int userId, int collectionId, int perfumeId, CancellationToken cancellationToken = default);

    Task RemovePerfumeAsync(int userId, int collectionId, int perfumeId, CancellationToken cancellationToken = default);
}
