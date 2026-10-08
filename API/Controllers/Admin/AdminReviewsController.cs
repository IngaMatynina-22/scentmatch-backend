using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public AdminReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
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
        return Ok(await _reviewService.ApproveAsync(id, cancellationToken));
    }

    [HttpPut("{id:int}/reject")]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminReviewDto>> Reject(int id, CancellationToken cancellationToken)
    {
        return Ok(await _reviewService.RejectAsync(id, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _reviewService.AdminDeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
