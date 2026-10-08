using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Application.Recommendations.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RecommendationsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecommendationsResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _recommendationService.GetRecommendationsAsync(userId, cancellationToken));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RecommendationsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecommendationsResponse>> Refresh(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _recommendationService.RefreshRecommendationsAsync(userId, cancellationToken));
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
