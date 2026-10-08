using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Reviews.DTOs;
using ScentMatch.Application.Reviews.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OwnReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnReviewDto>> Update(
        int id,
        [FromBody] UpdateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _reviewService.UpdateAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _reviewService.DeleteAsync(userId, id, cancellationToken);
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
