using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.History.Interfaces;
using ScentMatch.Application.Matching.DTOs;
using ScentMatch.Application.Matching.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/matching")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ISelectionHistoryService _selectionHistoryService;

    public MatchingController(
        IMatchingService matchingService,
        ISelectionHistoryService selectionHistoryService)
    {
        _matchingService = matchingService;
        _selectionHistoryService = selectionHistoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchingResponse>> Match(
        [FromBody] MatchingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _matchingService.MatchAsync(request, cancellationToken);
        await _selectionHistoryService.CreateFromMatchingAsync(userId, request, result.Items, cancellationToken);
        return Ok(result);
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
