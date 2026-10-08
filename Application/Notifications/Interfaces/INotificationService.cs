using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Notifications.DTOs;

namespace ScentMatch.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

    Task ClearAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification for a specific user. Intended for internal module use.
    /// </summary>
    Task<NotificationDto> CreateAsync(
        int userId,
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default);
}
