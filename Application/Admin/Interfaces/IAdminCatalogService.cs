using ScentMatch.Application.Admin.DTOs;

namespace ScentMatch.Application.Admin.Interfaces;

public interface IAdminCatalogService
{
    Task<CatalogQualityReportDto> GetQualityReportAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogDuplicateGroupDto>> GetDuplicatesAsync(CancellationToken cancellationToken = default);

    Task<CatalogImportResultDto> ImportAsync(
        CatalogImportRequest request,
        int adminUserId,
        CancellationToken cancellationToken = default);
}
