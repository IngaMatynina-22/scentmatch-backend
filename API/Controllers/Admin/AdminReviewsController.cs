using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Admin.Interfaces;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Reviews.DTOs;
using ScentMatch.Application.Reviews.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IAdminAuditService _auditService;

    public AdminReviewsController(IReviewService reviewService, IAdminAuditService auditService)
    {
        _reviewService = reviewService;
        _auditService = auditService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AdminReviewDto>>> GetReviews(
        [FromQuery] AdminReviewQueryParameters query,
        CancellationToken cancellationToken)
    {
        return Ok(await _reviewService.GetAdminReviewsAsync(query, cancellationToken));
    }

    [HttpPut("{id:int}/approve")]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminReviewDto>> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await _reviewService.ApproveAsync(id, cancellationToken);
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "ApproveReview",
            "Review",
            id.ToString(),
            cancellationToken: cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/reject")]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminReviewDto>> Reject(int id, CancellationToken cancellationToken)
    {
        var result = await _reviewService.RejectAsync(id, cancellationToken);
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "RejectReview",
            "Review",
            id.ToString(),
            cancellationToken: cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _reviewService.AdminDeleteAsync(id, cancellationToken);
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "DeleteReview",
            "Review",
            id.ToString(),
            cancellationToken: cancellationToken);
        return NoContent();
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
