using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Notifications.DTOs;
using ScentMatch.Application.Notifications.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly ScentMatchDbContext _dbContext;

    public NotificationService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<NotificationDto>> GetAsync(
        int userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                PerfumeId = x.PerfumeId,
                Type = x.Type,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<NotificationDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

        return new UnreadNotificationCountDto { Count = count };
    }

    public async Task MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException($"Notification with id {notificationId} was not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException($"Notification with id {notificationId} was not found.");
        }

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Notifications
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        _dbContext.Notifications.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationDto> CreateAsync(
        int userId,
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateCreateRequestAsync(userId, request, cancellationToken);

        var entity = BuildEntity(userId, request);
        _dbContext.Notifications.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task CreateManyAsync(
        IEnumerable<(int UserId, CreateNotificationRequest Request)> items,
        CancellationToken cancellationToken = default)
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            return;
        }

        foreach (var (userId, request) in list)
        {
            await ValidateCreateRequestAsync(userId, request, cancellationToken);
            _dbContext.Notifications.Add(BuildEntity(userId, request));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateCreateRequestAsync(
        int userId,
        CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!NotificationTypes.IsValid(request.Type))
        {
            throw new BadRequestException($"Type must be one of: {NotificationTypes.AllowedList}.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200)
        {
            throw new BadRequestException("Title is required and must be at most 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length > 4000)
        {
            throw new BadRequestException("Message is required and must be at most 4000 characters.");
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId, cancellationToken);

        if (!userExists)
        {
            throw new BadRequestException($"User with id {userId} was not found.");
        }

        if (request.PerfumeId.HasValue)
        {
            var perfumeExists = await _dbContext.Perfumes
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.PerfumeId.Value, cancellationToken);

            if (!perfumeExists)
            {
                throw new BadRequestException($"Perfume with id {request.PerfumeId.Value} was not found.");
            }
        }
    }

    private static Notification BuildEntity(int userId, CreateNotificationRequest request) => new()
    {
        UserId = userId,
        PerfumeId = request.PerfumeId,
        Type = NotificationTypes.Normalize(request.Type),
        Title = request.Title.Trim(),
        Message = request.Message.Trim(),
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    private static NotificationDto Map(Notification entity) => new()
    {
        Id = entity.Id,
        PerfumeId = entity.PerfumeId,
        Type = entity.Type,
        Title = entity.Title,
        Message = entity.Message,
        IsRead = entity.IsRead,
        CreatedAt = entity.CreatedAt
    };
}
