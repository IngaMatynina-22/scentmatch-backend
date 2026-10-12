using ScentMatch.Application.Admin.DTOs;

namespace ScentMatch.Application.Admin.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
