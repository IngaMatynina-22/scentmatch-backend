using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Catalog.Interfaces;

public interface ISeasonService
{
    Task<IReadOnlyList<SeasonDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SeasonDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SeasonDto> CreateAsync(CreateSeasonRequest request, CancellationToken cancellationToken = default);
    Task<SeasonDto> UpdateAsync(int id, UpdateSeasonRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
