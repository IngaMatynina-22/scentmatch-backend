using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Reviews.DTOs;
using ScentMatch.Application.Reviews.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/perfumes/{perfumeId:int}")]
public class PerfumeReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public PerfumeReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetReviews(
        int perfumeId,
        [FromQuery] ReviewQueryParameters query,
        CancellationToken cancellationToken)
    {
        return Ok(await _reviewService.GetApprovedReviewsAsync(perfumeId, query, cancellationToken));
    }

    [HttpPost("reviews")]
    [Authorize]
    [ProducesResponseType(typeof(OwnReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnReviewDto>> CreateReview(
        int perfumeId,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var created = await _reviewService.CreateAsync(userId, perfumeId, request, cancellationToken);
        return Created($"/api/perfumes/{perfumeId}/my-review", created);
    }

    [HttpGet("my-review")]
    [Authorize]
    [ProducesResponseType(typeof(OwnReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnReviewDto>> GetMyReview(
        int perfumeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _reviewService.GetMyReviewAsync(userId, perfumeId, cancellationToken));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User identity is invalid.");
        }

        return userId;
    }
}
