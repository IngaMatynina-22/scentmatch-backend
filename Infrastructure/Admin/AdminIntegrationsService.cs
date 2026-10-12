using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScentMatch.Application.Admin.DTOs;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Integrations;
using ScentMatch.Application.Options;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.PriceWatches;

namespace ScentMatch.Infrastructure.Admin;

public class AdminIntegrationsService : IAdminIntegrationsService
{
    private readonly ScentMatchDbContext _dbContext;
    private readonly IPriceProvider _priceProvider;
    private readonly IOptions<PriceTrackingOptions> _priceTrackingOptions;

    public AdminIntegrationsService(
        ScentMatchDbContext dbContext,
        IPriceProvider priceProvider,
        IOptions<PriceTrackingOptions> priceTrackingOptions)
    {
        _dbContext = dbContext;
        _priceProvider = priceProvider;
        _priceTrackingOptions = priceTrackingOptions;
    }

    public async Task<AdminIntegrationsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var options = _priceTrackingOptions.Value;

        var recentLogs = await _dbContext.BackgroundJobLogs
            .AsNoTracking()
            .Where(x => x.JobName == PriceWatchBackgroundService.JobName)
            .OrderByDescending(x => x.StartedAt)
            .Take(50)
            .Select(x => new
            {
                x.StartedAt,
                x.FinishedAt,
                x.Status,
                x.Message,
                x.ErrorCount
            })
            .ToListAsync(cancellationToken);

        var lastSuccess = recentLogs.FirstOrDefault(x =>
            x.Status is "Completed" or "Skipped");
        var lastError = recentLogs.FirstOrDefault(x =>
            x.Status is "Failed" or "CompletedWithErrors" || x.ErrorCount > 0);

        return new AdminIntegrationsDto
        {
            PriceTracking = new PriceTrackingIntegrationDto
            {
                Enabled = options.Enabled,
                CheckIntervalMinutes = options.CheckIntervalMinutes,
                ProviderName = _priceProvider.GetName(),
                ProviderConfigured = _priceProvider.IsConfigured,
                LastSuccessAt = lastSuccess?.FinishedAt ?? lastSuccess?.StartedAt,
                LastSuccessMessage = lastSuccess?.Message,
                LastErrorAt = lastError?.FinishedAt ?? lastError?.StartedAt,
                LastErrorMessage = lastError?.Message
            }
        };
    }
}
