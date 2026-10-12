using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Interfaces;

public interface IJournalService
{
    Task<PagedResult<JournalEntryDto>> GetEntriesAsync(
        int userId,
        JournalQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<JournalEntryDto> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default);

    Task<JournalEntryDto> CreateAsync(
        int userId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<JournalEntryDto> UpdateAsync(
        int userId,
        int id,
        UpdateJournalEntryRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default);
}
