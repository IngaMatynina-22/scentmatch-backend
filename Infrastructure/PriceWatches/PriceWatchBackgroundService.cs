using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScentMatch.Application.Integrations;
using ScentMatch.Application.Notifications.DTOs;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Application.Options;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.PriceWatches;

public class PriceWatchBackgroundService : BackgroundService
{
    public const string JobName = "PriceWatchCheck";

    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PerWatchDelay = TimeSpan.FromMilliseconds(500);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<PriceTrackingOptions> _options;
    private readonly ILogger<PriceWatchBackgroundService> _logger;

    public PriceWatchBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<PriceTrackingOptions> options,
        ILogger<PriceWatchBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
            {
                await DelaySafeAsync(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Price watch background job failed unexpectedly.");
            }

            var intervalMinutes = Math.Max(1, options.CheckIntervalMinutes);
            await DelaySafeAsync(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ScentMatchDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<IPriceProvider>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var jobLog = new BackgroundJobLog
        {
            JobName = JobName,
            StartedAt = DateTime.UtcNow,
            Status = "Running",
            Message = $"Starting price check via {provider.GetName()}."
        };
        db.BackgroundJobLogs.Add(jobLog);
        await db.SaveChangesAsync(cancellationToken);

        var processed = 0;
        var errors = 0;

        try
        {
            if (!provider.IsConfigured)
            {
                jobLog.Status = "Skipped";
                jobLog.FinishedAt = DateTime.UtcNow;
                jobLog.Message = $"Provider '{provider.GetName()}' is not configured; no prices invented.";
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            var watches = await db.PriceWatches
                .Include(x => x.Perfume)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.ShopLink)
                .Where(x => x.IsActive)
                .OrderBy(x => x.LastCheckedAt ?? DateTime.MinValue)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (var watch in watches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await CheckWatchAsync(db, provider, notifications, watch, cancellationToken);
                    processed++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(
                        ex,
                        "Failed checking price watch {WatchId} for perfume {PerfumeId}.",
                        watch.Id,
                        watch.PerfumeId);

                    watch.LastCheckedAt = DateTime.UtcNow;
                    watch.UpdatedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync(cancellationToken);
                await DelaySafeAsync(PerWatchDelay, cancellationToken);
            }

            jobLog.ProcessedCount = processed;
            jobLog.ErrorCount = errors;
            jobLog.FinishedAt = DateTime.UtcNow;
            jobLog.Status = errors > 0 ? "CompletedWithErrors" : "Completed";
            jobLog.Message = $"Checked {processed} watches with {errors} errors.";
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            jobLog.ProcessedCount = processed;
            jobLog.ErrorCount = errors;
            jobLog.FinishedAt = DateTime.UtcNow;
            jobLog.Status = "Cancelled";
            jobLog.Message = "Job cancelled.";
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            jobLog.ProcessedCount = processed;
            jobLog.ErrorCount = errors + 1;
            jobLog.FinishedAt = DateTime.UtcNow;
            jobLog.Status = "Failed";
            jobLog.Message = TruncateMessage($"Job failed: {ex.GetType().Name}: {SanitizeMessage(ex.Message)}");
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task CheckWatchAsync(
        ScentMatchDbContext db,
        IPriceProvider provider,
        INotificationService notifications,
        PriceWatch watch,
        CancellationToken cancellationToken)
    {
        var sourceUrl = watch.SourceUrl ?? watch.ShopLink?.Url;
        var sourceName = watch.SourceName
                         ?? watch.ShopLink?.StoreName
                         ?? provider.GetName();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProviderTimeout);

        var result = await provider.TryGetPriceAsync(
            new PriceProviderRequest
            {
                PerfumeId = watch.PerfumeId,
                PerfumeName = watch.Perfume.Name,
                BrandName = watch.Perfume.Brand?.Name,
                SourceUrl = sourceUrl,
                SourceName = sourceName,
                Currency = watch.Currency
            },
            timeoutCts.Token);

        var now = DateTime.UtcNow;
        watch.LastCheckedAt = now;
        watch.UpdatedAt = now;

        if (!result.IsConfigured || !result.Success || !result.Price.HasValue)
        {
            return;
        }

        var price = result.Price.Value;
        var currency = string.IsNullOrWhiteSpace(result.Currency)
            ? watch.Currency
            : result.Currency.Trim().ToUpperInvariant();

        db.PriceSnapshots.Add(new PriceSnapshot
        {
            PerfumeId = watch.PerfumeId,
            PriceWatchId = watch.Id,
            SourceName = Truncate(result.SourceName ?? sourceName, 100),
            SourceUrl = Truncate(result.SourceUrl ?? sourceUrl, 1000),
            Currency = currency,
            Price = price,
            CapturedAt = now
        });

        watch.LastKnownPrice = price;

        var shouldNotify = price <= watch.TargetPrice
                           && (!watch.LastNotifiedPrice.HasValue || price < watch.LastNotifiedPrice.Value);

        if (!shouldNotify)
        {
            return;
        }

        await notifications.CreateAsync(
            watch.UserId,
            new CreateNotificationRequest
            {
                Type = NotificationTypes.PriceDrop,
                Title = "Price drop alert",
                Message =
                    $"{watch.Perfume.Name} is now {price:0.00} {currency} (target {watch.TargetPrice:0.00} {watch.Currency}).",
                PerfumeId = watch.PerfumeId
            },
            cancellationToken);

        watch.LastNotifiedAt = now;
        watch.LastNotifiedPrice = price;
    }

    private static async Task DelaySafeAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unexpected error.";
        }

        // Avoid logging secrets or stack traces into job log Message.
        var cleaned = message
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        foreach (var marker in new[] { "password", "token", "apikey", "api_key", "secret", "authorization" })
        {
            if (cleaned.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return "An error occurred (details redacted).";
            }
        }

        return cleaned;
    }

    private static string TruncateMessage(string message) =>
        message.Length <= 2000 ? message : message[..1997] + "...";

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
