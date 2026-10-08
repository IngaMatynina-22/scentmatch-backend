using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Catalog.Interfaces;

public interface INoteService
{
    Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<NoteDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);
    Task<NoteDto> UpdateAsync(int id, UpdateNoteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
