using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Common.Models;
using ScentMatch.Application.Recommendations.DTOs;
using ScentMatch.Application.Recommendations.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/history/recommendations")]
[Authorize]
public class RecommendationHistoryController : ControllerBase
{
    private readonly IRecommendationHistoryService _historyService;

    public RecommendationHistoryController(IRecommendationHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RecommendationHistorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RecommendationHistorySummaryDto>>> GetBatches(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _historyService.GetBatchesAsync(userId, pagination, cancellationToken));
    }

    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(typeof(RecommendationHistoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationHistoryDetailsDto>> GetBatch(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _historyService.GetBatchByIdAsync(userId, batchId, cancellationToken));
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
