using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Common.Exceptions;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Application.Library.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/blind-comparison")]
[Authorize]
public class BlindComparisonController : ControllerBase
{
    private readonly IBlindComparisonService _blindComparisonService;

    public BlindComparisonController(IBlindComparisonService blindComparisonService)
    {
        _blindComparisonService = blindComparisonService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BlindComparisonSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlindComparisonSessionDto>> Create(
        [FromBody] CreateBlindComparisonRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var session = await _blindComparisonService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/blind-comparison/{session.Id}", session);
    }

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(BlindComparisonSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlindComparisonSessionDto>> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _blindComparisonService.GetSessionAsync(userId, sessionId, cancellationToken));
    }

    [HttpPut("{sessionId:guid}/ratings")]
    [ProducesResponseType(typeof(BlindComparisonSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlindComparisonSessionDto>> Rate(
        Guid sessionId,
        [FromBody] BlindComparisonRatingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _blindComparisonService.RateAsync(userId, sessionId, request, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(BlindComparisonResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlindComparisonResultsDto>> Complete(
        Guid sessionId,
        [FromBody] CompleteBlindComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _blindComparisonService.CompleteAsync(
            userId,
            sessionId,
            request ?? new CompleteBlindComparisonRequest(),
            cancellationToken));
    }

    [HttpGet("{sessionId:guid}/results")]
    [ProducesResponseType(typeof(BlindComparisonResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlindComparisonResultsDto>> GetResults(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(await _blindComparisonService.GetResultsAsync(userId, sessionId, cancellationToken));
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
