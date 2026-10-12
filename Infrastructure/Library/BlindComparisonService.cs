using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Catalog;
using ScentMatch.Infrastructure.Data;

namespace ScentMatch.Infrastructure.Library;

public class BlindComparisonService : IBlindComparisonService
{
    private readonly ScentMatchDbContext _dbContext;

    public BlindComparisonService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BlindComparisonSessionDto> CreateAsync(
        int userId,
        CreateBlindComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfumeIds = request.PerfumeIds.ToList();
        var activeCount = await _dbContext.Perfumes
            .AsNoTracking()
            .CountAsync(
                x => perfumeIds.Contains(x.Id) && x.IsActive && x.Brand.IsActive,
                cancellationToken);

        if (activeCount != perfumeIds.Count)
        {
            throw new NotFoundException("One or more perfumes were not found.");
        }

        Shuffle(perfumeIds);

        var now = DateTime.UtcNow;
        var session = new BlindComparisonSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = BlindComparisonStatuses.Active,
            CreatedAt = now,
            ExpiresAt = now.AddHours(24)
        };

        for (var i = 0; i < perfumeIds.Count; i++)
        {
            session.Slots.Add(new BlindComparisonSlot
            {
                PerfumeId = perfumeIds[i],
                Label = ((char)('A' + i)).ToString(),
                SortOrder = i
            });
        }

        _dbContext.BlindComparisonSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSessionDto(session);
    }

    public async Task<BlindComparisonSessionDto> GetSessionAsync(
        int userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, track: true, cancellationToken);
        await EnsureNotStaleActiveAsync(session, cancellationToken);
        return ToSessionDto(session);
    }

    public async Task<BlindComparisonSessionDto> RateAsync(
        int userId,
        Guid sessionId,
        BlindComparisonRatingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, track: true, cancellationToken);
        await EnsureActiveForMutationAsync(session, cancellationToken);

        var slotsByLabel = session.Slots
            .ToDictionary(x => x.Label, StringComparer.OrdinalIgnoreCase);

        foreach (var rating in request.Ratings)
        {
            if (!slotsByLabel.TryGetValue(rating.Label.Trim(), out var slot))
            {
                throw new BadRequestException($"Unknown label '{rating.Label}'.");
            }

            slot.Rating = rating.Rating;
            slot.Comment = string.IsNullOrWhiteSpace(rating.Comment)
                ? null
                : rating.Comment.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSessionDto(session);
    }

    public async Task<BlindComparisonResultsDto> CompleteAsync(
        int userId,
        Guid sessionId,
        CompleteBlindComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, track: true, cancellationToken);
        await EnsureActiveForMutationAsync(session, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.WinnerLabel))
        {
            var label = request.WinnerLabel.Trim();
            if (!session.Slots.Any(x => x.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException($"Unknown winner label '{request.WinnerLabel}'.");
            }

            session.WinnerLabel = session.Slots
                .First(x => x.Label.Equals(label, StringComparison.OrdinalIgnoreCase))
                .Label;
        }

        session.Status = BlindComparisonStatuses.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ToResultsDtoAsync(session, cancellationToken);
    }

    public async Task<BlindComparisonResultsDto> GetResultsAsync(
        int userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, track: true, cancellationToken);
        await EnsureNotStaleActiveAsync(session, cancellationToken);

        if (!string.Equals(session.Status, BlindComparisonStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Results are available only for completed sessions.");
        }

        return await ToResultsDtoAsync(session, cancellationToken);
    }

    private async Task<BlindComparisonSession> GetOwnedSessionAsync(
        int userId,
        Guid sessionId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track
            ? _dbContext.BlindComparisonSessions.Include(x => x.Slots).AsQueryable()
            : _dbContext.BlindComparisonSessions.AsNoTracking().Include(x => x.Slots);

        var session = await query.FirstOrDefaultAsync(
            x => x.Id == sessionId && x.UserId == userId,
            cancellationToken);

        if (session is null)
        {
            throw new NotFoundException($"Blind comparison session '{sessionId}' was not found.");
        }

        return session;
    }

    private async Task EnsureNotStaleActiveAsync(
        BlindComparisonSession session,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(session.Status, BlindComparisonStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            session.Status = BlindComparisonStatuses.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureActiveForMutationAsync(
        BlindComparisonSession session,
        CancellationToken cancellationToken)
    {
        await EnsureNotStaleActiveAsync(session, cancellationToken);

        if (string.Equals(session.Status, BlindComparisonStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Session is already completed.");
        }

        if (string.Equals(session.Status, BlindComparisonStatuses.Expired, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Session has expired.");
        }

        if (!string.Equals(session.Status, BlindComparisonStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Session is not active.");
        }
    }

    private static BlindComparisonSessionDto ToSessionDto(BlindComparisonSession session)
    {
        return new BlindComparisonSessionDto
        {
            Id = session.Id,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            CompletedAt = session.CompletedAt,
            WinnerLabel = session.WinnerLabel,
            Slots = session.Slots
                .OrderBy(x => x.SortOrder)
                .Select(x => new BlindComparisonSlotDto
                {
                    Label = x.Label,
                    SortOrder = x.SortOrder,
                    Rating = x.Rating,
                    Comment = x.Comment
                })
                .ToList()
        };
    }

    private async Task<BlindComparisonResultsDto> ToResultsDtoAsync(
        BlindComparisonSession session,
        CancellationToken cancellationToken)
    {
        var perfumeIds = session.Slots.Select(x => x.PerfumeId).Distinct().ToList();
        var perfumes = await PerfumeQueryHelper.ActivePerfumesWithDetails(_dbContext)
            .Where(x => perfumeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var perfumeMap = perfumes.ToDictionary(x => x.Id);

        return new BlindComparisonResultsDto
        {
            Id = session.Id,
            Status = session.Status,
            WinnerLabel = session.WinnerLabel,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            CompletedAt = session.CompletedAt,
            Slots = session.Slots
                .OrderBy(x => x.SortOrder)
                .Select(x => new BlindComparisonRevealedSlotDto
                {
                    Label = x.Label,
                    SortOrder = x.SortOrder,
                    PerfumeId = x.PerfumeId,
                    Perfume = perfumeMap.TryGetValue(x.PerfumeId, out var perfume)
                        ? CatalogMappings.ToListItemDto(perfume)
                        : null,
                    Rating = x.Rating,
                    Comment = x.Comment
                })
                .ToList()
        };
    }

    private static void Shuffle(IList<int> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
