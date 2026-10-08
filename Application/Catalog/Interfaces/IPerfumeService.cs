using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Catalog.Interfaces;

public interface IPerfumeService
{
    Task<PagedResult<PerfumeListItemDto>> GetAllAsync(
        PerfumeQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<PerfumeDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PerfumeDetailsDto> CreateAsync(CreatePerfumeRequest request, CancellationToken cancellationToken = default);
    Task<PerfumeDetailsDto> UpdateAsync(int id, UpdatePerfumeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
