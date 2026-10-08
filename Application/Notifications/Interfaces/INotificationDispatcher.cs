using ScentMatch.Domain.Entities;

namespace ScentMatch.Application.Notifications.Interfaces;

/// <summary>
/// Creates activity notifications for domain events (catalog, reviews, recommendations).
/// </summary>
public interface INotificationDispatcher
{
    Task NotifyNewPerfumeAsync(Perfume perfume, CancellationToken cancellationToken = default);

    Task NotifyCatalogUpdatedAsync(Perfume perfume, CancellationToken cancellationToken = default);

    Task NotifyRatingChangedAsync(
        Perfume perfume,
        decimal previousRating,
        decimal newRating,
        CancellationToken cancellationToken = default);

    Task NotifyReviewModeratedAsync(
        Review review,
        string perfumeName,
        CancellationToken cancellationToken = default);

    Task NotifyRecommendationsReadyAsync(
        int userId,
        int itemCount,
        CancellationToken cancellationToken = default);
}
