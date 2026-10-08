using Microsoft.EntityFrameworkCore;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Reviews.DTOs;
using ScentMatch.Application.Reviews.Interfaces;
using ScentMatch.Domain.Constants;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data;
using ScentMatch.Infrastructure.Library;

namespace ScentMatch.Infrastructure.Reviews;

public class ReviewService : IReviewService
{
    private readonly ScentMatchDbContext _dbContext;

    public ReviewService(ScentMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ReviewDto>> GetApprovedReviewsAsync(
        int perfumeId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureActivePerfumeAsync(perfumeId, cancellationToken);

        var query = _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.PerfumeId == perfumeId && x.Status == ReviewStatuses.Approved);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => new ReviewDto
            {
                Id = x.Id,
                Rating = x.Rating,
                Text = x.Text,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                User = new ReviewUserDto
                {
                    Id = x.User.Id,
                    Username = x.User.UserName ?? string.Empty
                }
            })
            .ToListAsync(cancellationToken);

        return PagedResult<ReviewDto>.Create(items, pagination.Page, pagination.PageSize, totalItems);
    }

    public async Task<OwnReviewDto> GetMyReviewAsync(
        int userId,
        int perfumeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActivePerfumeAsync(perfumeId, cancellationToken);

        var review = await _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.PerfumeId == perfumeId)
            .Select(x => new OwnReviewDto
            {
                Id = x.Id,
                PerfumeId = x.PerfumeId,
                Rating = x.Rating,
                Text = x.Text,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                User = new ReviewUserDto
                {
                    Id = x.User.Id,
                    Username = x.User.UserName ?? string.Empty
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (review is null)
        {
            throw new NotFoundException("Review was not found.");
        }

        return review;
    }

    public async Task<OwnReviewDto> CreateAsync(
        int userId,
        int perfumeId,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureActivePerfumeAsync(perfumeId, cancellationToken);

        var exists = await _dbContext.Reviews
            .AnyAsync(x => x.UserId == userId && x.PerfumeId == perfumeId, cancellationToken);

        if (exists)
        {
            throw new ConflictException("You have already reviewed this perfume.");
        }

        var perfume = await _dbContext.Perfumes
            .FirstAsync(x => x.Id == perfumeId, cancellationToken);

        var review = new Review
        {
            UserId = userId,
            PerfumeId = perfumeId,
            Rating = request.Rating,
            Text = NormalizeText(request.Text),
            Status = ReviewStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Reviews.Add(review);

        await RecalculatePerfumeRatingAsync(perfume, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("You have already reviewed this perfume.");
        }

        return await MapOwnReviewAsync(review.Id, cancellationToken);
    }

    public async Task<OwnReviewDto> UpdateAsync(
        int userId,
        int reviewId,
        UpdateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(x => x.Id == reviewId && x.UserId == userId, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException($"Review with id {reviewId} was not found.");
        }

        var perfume = await _dbContext.Perfumes
            .FirstAsync(x => x.Id == review.PerfumeId, cancellationToken);

        review.Rating = request.Rating;
        review.Text = NormalizeText(request.Text);
        review.Status = ReviewStatuses.Pending;
        review.UpdatedAt = DateTime.UtcNow;

        await RecalculatePerfumeRatingAsync(perfume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapOwnReviewAsync(review.Id, cancellationToken);
    }

    public async Task DeleteAsync(int userId, int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(x => x.Id == reviewId && x.UserId == userId, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException($"Review with id {reviewId} was not found.");
        }

        var perfume = await _dbContext.Perfumes
            .FirstAsync(x => x.Id == review.PerfumeId, cancellationToken);

        _dbContext.Reviews.Remove(review);
        await RecalculatePerfumeRatingAsync(perfume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminReviewDto>> GetAdminReviewsAsync(
        AdminReviewQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(query.Status)
            ? ReviewStatuses.Pending
            : ReviewStatuses.Normalize(query.Status);

        var reviewsQuery = _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.Status == status);

        var totalItems = await reviewsQuery.CountAsync(cancellationToken);

        var items = await reviewsQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminReviewDto
            {
                Id = x.Id,
                Rating = x.Rating,
                Text = x.Text,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                User = new AdminReviewUserDto
                {
                    Id = x.User.Id,
                    Username = x.User.UserName ?? string.Empty,
                    Email = x.User.Email ?? string.Empty
                },
                Perfume = new AdminReviewPerfumeDto
                {
                    Id = x.Perfume.Id,
                    Name = x.Perfume.Name
                }
            })
            .ToListAsync(cancellationToken);

        return PagedResult<AdminReviewDto>.Create(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<AdminReviewDto> ApproveAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await GetReviewOrThrowAsync(reviewId, cancellationToken);
        var perfume = await _dbContext.Perfumes.FirstAsync(x => x.Id == review.PerfumeId, cancellationToken);

        review.Status = ReviewStatuses.Approved;
        review.UpdatedAt = DateTime.UtcNow;

        await RecalculatePerfumeRatingAsync(perfume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAdminReviewAsync(review.Id, cancellationToken);
    }

    public async Task<AdminReviewDto> RejectAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await GetReviewOrThrowAsync(reviewId, cancellationToken);
        var perfume = await _dbContext.Perfumes.FirstAsync(x => x.Id == review.PerfumeId, cancellationToken);

        review.Status = ReviewStatuses.Rejected;
        review.UpdatedAt = DateTime.UtcNow;

        await RecalculatePerfumeRatingAsync(perfume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAdminReviewAsync(review.Id, cancellationToken);
    }

    public async Task AdminDeleteAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await GetReviewOrThrowAsync(reviewId, cancellationToken);
        var perfume = await _dbContext.Perfumes.FirstAsync(x => x.Id == review.PerfumeId, cancellationToken);

        _dbContext.Reviews.Remove(review);
        await RecalculatePerfumeRatingAsync(perfume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureActivePerfumeAsync(int perfumeId, CancellationToken cancellationToken)
    {
        var exists = await PerfumeQueryHelper.IsActivePerfumeAsync(_dbContext, perfumeId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Perfume with id {perfumeId} was not found.");
        }
    }

    private async Task<Review> GetReviewOrThrowAsync(int reviewId, CancellationToken cancellationToken)
    {
        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException($"Review with id {reviewId} was not found.");
        }

        return review;
    }

    private async Task RecalculatePerfumeRatingAsync(Perfume perfume, CancellationToken cancellationToken)
    {
        // Load into tracker so pending Added/Modified/Deleted states are reflected
        // before a single SaveChanges persists review + perfume aggregate together.
        await _dbContext.Reviews
            .Where(x => x.PerfumeId == perfume.Id)
            .LoadAsync(cancellationToken);

        var approvedRatings = _dbContext.Reviews.Local
            .Where(x => x.PerfumeId == perfume.Id
                        && x.Status == ReviewStatuses.Approved
                        && _dbContext.Entry(x).State != EntityState.Deleted)
            .Select(x => x.Rating)
            .ToList();

        if (approvedRatings.Count == 0)
        {
            perfume.Rating = 0m;
            perfume.ReviewsCount = 0;
        }
        else
        {
            perfume.ReviewsCount = approvedRatings.Count;
            perfume.Rating = Math.Round(
                (decimal)approvedRatings.Average(),
                2,
                MidpointRounding.AwayFromZero);
        }

        perfume.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<OwnReviewDto> MapOwnReviewAsync(int reviewId, CancellationToken cancellationToken)
    {
        var dto = await _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.Id == reviewId)
            .Select(x => new OwnReviewDto
            {
                Id = x.Id,
                PerfumeId = x.PerfumeId,
                Rating = x.Rating,
                Text = x.Text,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                User = new ReviewUserDto
                {
                    Id = x.User.Id,
                    Username = x.User.UserName ?? string.Empty
                }
            })
            .FirstAsync(cancellationToken);

        return dto;
    }

    private async Task<AdminReviewDto> MapAdminReviewAsync(int reviewId, CancellationToken cancellationToken)
    {
        return await _dbContext.Reviews
            .AsNoTracking()
            .Where(x => x.Id == reviewId)
            .Select(x => new AdminReviewDto
            {
                Id = x.Id,
                Rating = x.Rating,
                Text = x.Text,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                User = new AdminReviewUserDto
                {
                    Id = x.User.Id,
                    Username = x.User.UserName ?? string.Empty,
                    Email = x.User.Email ?? string.Empty
                },
                Perfume = new AdminReviewPerfumeDto
                {
                    Id = x.Perfume.Id,
                    Name = x.Perfume.Name
                }
            })
            .FirstAsync(cancellationToken);
    }

    private static string? NormalizeText(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
