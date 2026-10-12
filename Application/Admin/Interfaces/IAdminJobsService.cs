using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Admin.Interfaces;

public interface IAdminJobsService
{
    Task<PagedResult<BackgroundJobLogDto>> GetRecentAsync(
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);
}
