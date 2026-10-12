using ScentMatch.Application.Admin.DTOs;

namespace ScentMatch.Application.Admin.Interfaces;

public interface IAdminIntegrationsService
{
    Task<AdminIntegrationsDto> GetAsync(CancellationToken cancellationToken = default);
}
