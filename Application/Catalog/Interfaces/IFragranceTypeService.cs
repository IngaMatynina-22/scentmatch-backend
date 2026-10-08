using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Catalog.Interfaces;

public interface IFragranceTypeService
{
    Task<IReadOnlyList<FragranceTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FragranceTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FragranceTypeDto> CreateAsync(CreateFragranceTypeRequest request, CancellationToken cancellationToken = default);
    Task<FragranceTypeDto> UpdateAsync(int id, UpdateFragranceTypeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
