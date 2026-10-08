using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Reviews.DTOs;

namespace ScentMatch.Application.Reviews.Interfaces;

public interface IReviewService
{
    Task<PagedResult<ReviewDto>> GetApprovedReviewsAsync(
        int perfumeId,
        ReviewQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OwnReviewDto>> GetMyReviewsAsync(
        int userId,
        ReviewQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<OwnReviewDto> GetMyReviewAsync(
        int userId,
        int perfumeId,
        CancellationToken cancellationToken = default);

    Task<OwnReviewDto> CreateAsync(
        int userId,
        int perfumeId,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<OwnReviewDto> UpdateAsync(
        int userId,
        int reviewId,
        UpdateReviewRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, int reviewId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminReviewDto>> GetAdminReviewsAsync(
        AdminReviewQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<AdminReviewDto> ApproveAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<AdminReviewDto> RejectAsync(int reviewId, CancellationToken cancellationToken = default);

    Task AdminDeleteAsync(int reviewId, CancellationToken cancellationToken = default);
}
