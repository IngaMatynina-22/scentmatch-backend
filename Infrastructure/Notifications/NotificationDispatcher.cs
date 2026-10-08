using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Notifications.DTOs;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly ScentMatchDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public NotificationDispatcher(
        ScentMatchDbContext dbContext,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task NotifyNewPerfumeAsync(Perfume perfume, CancellationToken cancellationToken = default)
    {
        // Notify users who configured preferences (interested in catalog updates).
        var userIds = await _dbContext.UserPreferences
            .AsNoTracking()
            .Where(x => x.User.IsActive)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        await _notificationService.CreateManyAsync(
            userIds.Select(userId => (
                userId,
                new CreateNotificationRequest
                {
                    Type = NotificationTypes.NewPerfume,
                    Title = "New perfume in catalog",
                    Message = $"A new perfume \"{perfume.Name}\" was added to the catalog.",
                    PerfumeId = perfume.Id
                })),
            cancellationToken);
    }

    public async Task NotifyCatalogUpdatedAsync(Perfume perfume, CancellationToken cancellationToken = default)
    {
        var userIds = await GetFavoriteUserIdsAsync(perfume.Id, cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        await _notificationService.CreateManyAsync(
            userIds.Select(userId => (
                userId,
                new CreateNotificationRequest
                {
                    Type = NotificationTypes.CatalogUpdate,
                    Title = "Favorite perfume updated",
                    Message = $"\"{perfume.Name}\" in your favorites was updated in the catalog.",
                    PerfumeId = perfume.Id
                })),
            cancellationToken);
    }

    public async Task NotifyRatingChangedAsync(
        Perfume perfume,
        decimal previousRating,
        decimal newRating,
        CancellationToken cancellationToken = default)
    {
        if (previousRating == newRating)
        {
            return;
        }

        var userIds = await GetFavoriteUserIdsAsync(perfume.Id, cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        await _notificationService.CreateManyAsync(
            userIds.Select(userId => (
                userId,
                new CreateNotificationRequest
                {
                    Type = NotificationTypes.RatingChanged,
                    Title = "Perfume rating updated",
                    Message =
                        $"The average rating of \"{perfume.Name}\" changed from {previousRating:0.##} to {newRating:0.##}.",
                    PerfumeId = perfume.Id
                })),
            cancellationToken);
    }

    public async Task NotifyReviewModeratedAsync(
        Review review,
        string perfumeName,
        CancellationToken cancellationToken = default)
    {
        var statusLabel = review.Status.Equals(ReviewStatuses.Approved, StringComparison.OrdinalIgnoreCase)
            ? "approved"
            : "rejected";

        await _notificationService.CreateAsync(
            review.UserId,
            new CreateNotificationRequest
            {
                Type = NotificationTypes.Review,
                Title = $"Review {statusLabel}",
                Message = $"Your review for \"{perfumeName}\" was {statusLabel}.",
                PerfumeId = review.PerfumeId
            },
            cancellationToken);
    }

    public async Task NotifyRecommendationsReadyAsync(
        int userId,
        int itemCount,
        CancellationToken cancellationToken = default)
    {
        if (itemCount <= 0)
        {
            return;
        }

        await _notificationService.CreateAsync(
            userId,
            new CreateNotificationRequest
            {
                Type = NotificationTypes.Recommendation,
                Title = "New personalized recommendations",
                Message = $"We prepared {itemCount} perfume recommendation(s) based on your preferences and ratings."
            },
            cancellationToken);
    }

    private async Task<List<int>> GetFavoriteUserIdsAsync(int perfumeId, CancellationToken cancellationToken)
    {
        return await _dbContext.FavoritePerfumes
            .AsNoTracking()
            .Where(x => x.PerfumeId == perfumeId && x.User.IsActive)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
